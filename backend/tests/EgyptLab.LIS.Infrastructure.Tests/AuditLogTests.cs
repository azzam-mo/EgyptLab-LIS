using System.Text.Json;
using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Infrastructure.Tests;

/// <summary>
/// Audit rows are written by the SaveChanges interceptor in the same
/// transaction as the clinical or financial change. Tests use synthetic data
/// only and inspect the persisted before/after JSON.
/// </summary>
public sealed class AuditLogTests
{
    private const string Redacted = "[redacted]";

    [Fact]
    public async Task PatientUpdate_RecordsChangedFieldsAndRedactsDirectIdentifiers()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchId, "A");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(actorId, branchId, role: null, ipAddress: "127.0.0.1");

            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
                patient.FirstName = "Changed";
                patient.Phone = "01111111111";
                patient.Sex = "M";
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            var audit = await verify.AuditLog.SingleAsync(
                item => item.EntityId == patientId && item.Action == "update");

            Assert.Equal("Patient", audit.EntityName);
            Assert.Equal(branchId, audit.BranchId);
            Assert.Equal(actorId, audit.ActorUserId);
            Assert.Null(audit.ActorRole);
            Assert.Equal("127.0.0.1", audit.IpAddress);

            using var before = JsonDocument.Parse(audit.BeforeJson!);
            using var after = JsonDocument.Parse(audit.AfterJson!);

            Assert.Equal("F", before.RootElement.GetProperty("Sex").GetString());
            Assert.Equal("M", after.RootElement.GetProperty("Sex").GetString());
            Assert.Equal(Redacted, before.RootElement.GetProperty("FirstName").GetString());
            Assert.Equal(Redacted, after.RootElement.GetProperty("FirstName").GetString());
            Assert.Equal(Redacted, before.RootElement.GetProperty("Phone").GetString());
            Assert.Equal(Redacted, after.RootElement.GetProperty("Phone").GetString());

            // Only changed properties are recorded. Unchanged identifiers must
            // not leak into the snapshot at all.
            Assert.False(before.RootElement.TryGetProperty("NationalId", out _));
            Assert.False(after.RootElement.TryGetProperty("NationalId", out _));
            Assert.DoesNotContain("Synthetic", audit.BeforeJson!, StringComparison.Ordinal);
            Assert.DoesNotContain("Changed", audit.AfterJson!, StringComparison.Ordinal);
            Assert.DoesNotContain("01111111111", audit.AfterJson!, StringComparison.Ordinal);
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task ResultUpdate_RecordsBeforeAndAfterValues()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var (_, resultId) = await SeedResultGraphAsync(database, branchId);

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(actorId, branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var result = await context.Results.SingleAsync(item => item.Id == resultId);
                result.ValueText = "Final synthetic value";
                result.Status = "Final";
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            var audit = await verify.AuditLog.SingleAsync(
                item => item.EntityId == resultId && item.Action == "update");

            using var before = JsonDocument.Parse(audit.BeforeJson!);
            using var after = JsonDocument.Parse(audit.AfterJson!);

            Assert.Equal("Initial synthetic value", before.RootElement.GetProperty("ValueText").GetString());
            Assert.Equal("Final synthetic value", after.RootElement.GetProperty("ValueText").GetString());
            Assert.Equal("Draft", before.RootElement.GetProperty("Status").GetString());
            Assert.Equal("Final", after.RootElement.GetProperty("Status").GetString());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task InvoiceUpdate_RecordsBeforeAndAfterValues()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var invoiceId = await SeedInvoiceGraphAsync(database, branchId);

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(actorId, branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var invoice = await context.Invoices.SingleAsync(item => item.Id == invoiceId);
                invoice.PaidPiaster = 2500;
                invoice.Status = "PartiallyPaid";
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            var audit = await verify.AuditLog.SingleAsync(
                item => item.EntityId == invoiceId && item.Action == "update");

            using var before = JsonDocument.Parse(audit.BeforeJson!);
            using var after = JsonDocument.Parse(audit.AfterJson!);

            Assert.Equal(0, before.RootElement.GetProperty("PaidPiaster").GetInt64());
            Assert.Equal(2500, after.RootElement.GetProperty("PaidPiaster").GetInt64());
            Assert.Equal("Open", before.RootElement.GetProperty("Status").GetString());
            Assert.Equal("PartiallyPaid", after.RootElement.GetProperty("Status").GetString());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task MultipleAuditedEntitiesInOneSave_ProduceOneRowEach()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchId, "A");
        var invoiceId = await SeedInvoiceGraphAsync(database, branchId);

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
                var invoice = await context.Invoices.SingleAsync(item => item.Id == invoiceId);
                patient.Sex = "M";
                invoice.PaidPiaster = 100;
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            var audits = await verify.AuditLog
                .Where(item => item.Action == "update")
                .ToListAsync();

            Assert.Equal(2, audits.Count);
            Assert.Contains(audits, item => item.EntityId == patientId && item.EntityName == "Patient");
            Assert.Contains(audits, item => item.EntityId == invoiceId && item.EntityName == "Invoice");
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task AuditInsertFailure_RollsBackBusinessChange()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchId, "A");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using var context = database.CreateContext();
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER fail_audit_insert
                BEFORE INSERT ON audit_log
                BEGIN
                    SELECT RAISE(ABORT, 'forced audit failure');
                END;
                """);

            var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
            patient.Phone = "01111111111";

            await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());

            // The business change must have rolled back with the failed audit
            // insert, even though the tracked entity is still modified.
            using (var verifyRollback = database.CreateContext())
            {
                var reloaded = await verifyRollback.Patients.SingleAsync(item => item.Id == patientId);
                Assert.NotEqual("01111111111", reloaded.Phone);
                Assert.Empty(await verifyRollback.AuditLog
                    .Where(item => item.EntityId == patientId && item.Action == "update")
                    .ToListAsync());
            }

            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_audit_insert;");

            // Retrying after the audit failure must create exactly one audit
            // row, not a duplicate left over from the failed attempt.
            await context.SaveChangesAsync();

            using var verifyRetry = database.CreateContext();
            var saved = await verifyRetry.Patients.SingleAsync(item => item.Id == patientId);
            Assert.Equal("01111111111", saved.Phone);
            Assert.Single(await verifyRetry.AuditLog
                .Where(item => item.EntityId == patientId && item.Action == "update")
                .ToListAsync());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task AuditLogUpdatesAndDeletes_AreRejectedByEfAndDatabaseTriggers()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchId, "A");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
                patient.Sex = "M";
                await context.SaveChangesAsync();
            }

            using (var updateContext = database.CreateContext())
            {
                var audit = await updateContext.AuditLog.SingleAsync(
                    item => item.EntityId == patientId && item.Action == "update");
                audit.Reason = "tampered";

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => updateContext.SaveChangesAsync());
                Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
            }

            using (var deleteContext = database.CreateContext())
            {
                var audit = await deleteContext.AuditLog.SingleAsync(
                    item => item.EntityId == patientId && item.Action == "update");
                deleteContext.AuditLog.Remove(audit);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => deleteContext.SaveChangesAsync());
                Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
            }

            using (var sqlContext = database.CreateContext())
            {
                var updateException = await Assert.ThrowsAsync<SqliteException>(() =>
                    sqlContext.Database.ExecuteSqlRawAsync(
                        "UPDATE audit_log SET Reason = 'tampered' WHERE EntityId = {0};",
                        patientId));
                Assert.Contains("append-only", updateException.Message, StringComparison.OrdinalIgnoreCase);

                var deleteException = await Assert.ThrowsAsync<SqliteException>(() =>
                    sqlContext.Database.ExecuteSqlRawAsync(
                        "DELETE FROM audit_log WHERE EntityId = {0};",
                        patientId));
                Assert.Contains("append-only", deleteException.Message, StringComparison.OrdinalIgnoreCase);
            }

            using var verify = database.CreateContext();
            var persisted = await verify.AuditLog.SingleAsync(
                item => item.EntityId == patientId && item.Action == "update");
            Assert.Null(persisted.Reason);
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task DirectAuditInsertAndBranchChange_AreRejected()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchId, "A");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                context.AuditLog.Add(new AuditLog
                {
                    BranchId = branchId,
                    EntityName = "Patient",
                    EntityId = patientId,
                    Action = "update",
                    ActorUserId = Guid.NewGuid(),
                    BeforeJson = "{}",
                    AfterJson = "{}"
                });

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => context.SaveChangesAsync());
                Assert.Contains("direct inserts", exception.Message, StringComparison.OrdinalIgnoreCase);
            }

            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
                patient.BranchId = Guid.NewGuid();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => context.SaveChangesAsync());
                Assert.Contains("branch", exception.Message, StringComparison.OrdinalIgnoreCase);
            }

            using var verify = database.CreateContext();
            var stored = await verify.Patients.SingleAsync(item => item.Id == patientId);
            Assert.Equal(branchId, stored.BranchId);
            Assert.Empty(await verify.AuditLog
                .Where(item => item.EntityId == patientId && item.Action == "update")
                .ToListAsync());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task HardDeleteOfAuditedEntities_IsRejected()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var (patientId, resultId) = await SeedResultGraphAsync(database, branchId);
        var invoiceId = await SeedInvoiceGraphAsync(database, branchId);

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            await AssertHardDeleteRejectedAsync(database, context =>
                context.Patients.Single(item => item.Id == patientId));

            await AssertHardDeleteRejectedAsync(database, context =>
                context.Results.Single(item => item.Id == resultId));

            await AssertHardDeleteRejectedAsync(database, context =>
                context.Invoices.Single(item => item.Id == invoiceId));
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task CreateAndSoftDelete_ProduceTraceableAuditRows()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            var patient = NewPatient(branchId, "Create");
            using (var context = database.CreateContext())
            {
                context.Patients.Add(patient);
                await context.SaveChangesAsync();
            }

            using (var createVerify = database.CreateContext())
            {
                var createAudit = await createVerify.AuditLog.SingleAsync(
                    item => item.EntityId == patient.Id && item.Action == "create");
                Assert.Null(createAudit.BeforeJson);
                Assert.NotNull(createAudit.AfterJson);
                Assert.DoesNotContain("Synthetic", createAudit.AfterJson!, StringComparison.Ordinal);
                Assert.DoesNotContain(patient.NationalId!, createAudit.AfterJson!, StringComparison.Ordinal);
                Assert.DoesNotContain(patient.Phone!, createAudit.AfterJson!, StringComparison.Ordinal);
            }

            using (var deleteContext = database.CreateContext())
            {
                var stored = await deleteContext.Patients.SingleAsync(item => item.Id == patient.Id);
                stored.DeletedAt = DateTimeOffset.UtcNow;
                await deleteContext.SaveChangesAsync();
            }

            using (var deleteVerify = database.CreateContext())
            {
                Assert.Empty(await deleteVerify.Patients
                    .Where(item => item.Id == patient.Id)
                    .ToListAsync());

                var deleteAudit = await deleteVerify.AuditLog.SingleAsync(
                    item => item.EntityId == patient.Id && item.Action == "delete");
                using var before = JsonDocument.Parse(deleteAudit.BeforeJson!);
                using var after = JsonDocument.Parse(deleteAudit.AfterJson!);
                Assert.Equal(JsonValueKind.Null, before.RootElement.GetProperty("DeletedAt").ValueKind);
                Assert.NotEqual(JsonValueKind.Null, after.RootElement.GetProperty("DeletedAt").ValueKind);

                Assert.Single(await deleteVerify.Patients.IgnoreQueryFilters()
                    .Where(item => item.Id == patient.Id)
                    .ToListAsync());
            }
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task ResultAndInvoiceCreationAndSoftDeletion_AreAudited()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var (_, resultId) = await SeedResultGraphAsync(database, branchId);
        var invoiceId = await SeedInvoiceGraphAsync(database, branchId);

        using (var createVerify = database.CreateContext())
        {
            Assert.Single(await createVerify.AuditLog.IgnoreQueryFilters()
                .Where(item => item.EntityId == resultId && item.Action == "create")
                .ToListAsync());
            Assert.Single(await createVerify.AuditLog.IgnoreQueryFilters()
                .Where(item => item.EntityId == invoiceId && item.Action == "create")
                .ToListAsync());
        }

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var result = await context.Results.SingleAsync(item => item.Id == resultId);
                var invoice = await context.Invoices.SingleAsync(item => item.Id == invoiceId);
                result.DeletedAt = DateTimeOffset.UtcNow;
                invoice.DeletedAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            var resultDelete = await verify.AuditLog.SingleAsync(
                item => item.EntityId == resultId && item.Action == "delete");
            var invoiceDelete = await verify.AuditLog.SingleAsync(
                item => item.EntityId == invoiceId && item.Action == "delete");

            Assert.Equal("Result", resultDelete.EntityName);
            Assert.Equal("Invoice", invoiceDelete.EntityName);
            Assert.Empty(await verify.Results.Where(item => item.Id == resultId).ToListAsync());
            Assert.Empty(await verify.Invoices.Where(item => item.Id == invoiceId).ToListAsync());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task CrossBranchUpdate_IsRejectedAndProducesNoAuditRow()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        var patientId = await SeedPatientAsync(database, branchA, "A");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchA);
            AuditActor.Set(Guid.NewGuid(), branchB, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientId);
                patient.Sex = "M";

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => context.SaveChangesAsync());
                Assert.Contains("actor branch", exception.Message, StringComparison.OrdinalIgnoreCase);
            }

            using var verify = database.CreateContext();
            var reloaded = await verify.Patients.SingleAsync(item => item.Id == patientId);
            Assert.Equal("F", reloaded.Sex);
            Assert.Empty(await verify.AuditLog
                .Where(item => item.EntityId == patientId && item.Action == "update")
                .ToListAsync());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task AuditRows_AreBranchScopedAndFailClosedWithoutScope()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        var patientA = await SeedPatientAsync(database, branchA, "A");
        var patientB = await SeedPatientAsync(database, branchB, "B");

        var scope = new BranchScope();
        try
        {
            scope.Set(branchA);
            AuditActor.Set(Guid.NewGuid(), branchA, role: null, ipAddress: null);
            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientA);
                patient.Sex = "M";
                await context.SaveChangesAsync();
            }

            scope.Set(branchB);
            AuditActor.Set(Guid.NewGuid(), branchB, role: null, ipAddress: null);
            using (var context = database.CreateContext())
            {
                var patient = await context.Patients.SingleAsync(item => item.Id == patientB);
                patient.Sex = "M";
                await context.SaveChangesAsync();
            }

            scope.Set(branchA);
            using (var context = database.CreateContext())
            {
                var audits = await context.AuditLog
                    .Where(item => item.Action == "update")
                    .ToListAsync();
                var audit = Assert.Single(audits);
                Assert.Equal(branchA, audit.BranchId);
                Assert.Equal(patientA, audit.EntityId);
            }

            scope.Set(branchB);
            using (var context = database.CreateContext())
            {
                var audits = await context.AuditLog
                    .Where(item => item.Action == "update")
                    .ToListAsync();
                var audit = Assert.Single(audits);
                Assert.Equal(branchB, audit.BranchId);
                Assert.Equal(patientB, audit.EntityId);
            }

            scope.Clear();
            using (var context = database.CreateContext())
            {
                Assert.Empty(await context.AuditLog.ToListAsync());
            }

            using (var unscoped = database.CreateContext())
            {
                Assert.Equal(
                    2,
                    await unscoped.AuditLog.IgnoreQueryFilters()
                        .CountAsync(item => item.Action == "update"));
            }
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    [Fact]
    public async Task NonAuditedEntityUpdates_DoNotCreateAuditRows()
    {
        await using var database = await AuditDatabase.CreateAsync();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var seed = database.CreateContext())
        {
            seed.Users.Add(new User
            {
                Id = userId,
                BranchId = branchId,
                UserName = "synthetic.user",
                NormalizedUserName = "SYNTHETIC.USER",
                FullName = "Synthetic User",
                PasswordHash = "synthetic-hash",
                SecurityStamp = "synthetic-stamp"
            });
            await seed.SaveChangesAsync();
        }

        var scope = new BranchScope();
        try
        {
            scope.Set(branchId);
            AuditActor.Set(Guid.NewGuid(), branchId, role: null, ipAddress: null);

            using (var context = database.CreateContext())
            {
                var user = await context.Users.SingleAsync(item => item.Id == userId);
                user.FullName = "Synthetic User Changed";
                await context.SaveChangesAsync();
            }

            using var verify = database.CreateContext();
            Assert.Empty(await verify.AuditLog.ToListAsync());
        }
        finally
        {
            AuditActor.Clear();
            scope.Clear();
        }
    }

    private static async Task<Guid> SeedPatientAsync(
        AuditDatabase database,
        Guid branchId,
        string suffix)
    {
        var patient = NewPatient(branchId, suffix);
        using var context = database.CreateContext();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient.Id;
    }

    private static Patient NewPatient(Guid branchId, string suffix) => new()
    {
        BranchId = branchId,
        FirstName = "Synthetic",
        LastName = $"Patient-{suffix}",
        NationalId = $"SYN-{suffix}-{Guid.NewGuid():N}",
        Phone = $"0100000{Guid.NewGuid():N}"[..12],
        DateOfBirth = new DateOnly(1990, 1, 1),
        Sex = "F"
    };

    private static async Task<(Guid PatientId, Guid ResultId)> SeedResultGraphAsync(
        AuditDatabase database,
        Guid branchId)
    {
        var patient = NewPatient(branchId, "Result");
        var test = new LabTest
        {
            BranchId = branchId,
            Code = $"T-{Guid.NewGuid():N}",
            Name = "Synthetic Test",
            PricePiaster = 1000
        };
        var order = new Order
        {
            BranchId = branchId,
            Patient = patient,
            PatientId = patient.Id,
            OrderNumber = $"O-{Guid.NewGuid():N}"
        };
        var item = new OrderItem
        {
            BranchId = branchId,
            Order = order,
            OrderId = order.Id,
            Test = test,
            TestId = test.Id,
            Quantity = 1,
            UnitPricePiaster = 1000,
            LineTotalPiaster = 1000
        };
        var sample = new Sample
        {
            BranchId = branchId,
            Order = order,
            OrderId = order.Id,
            Barcode = $"S-{Guid.NewGuid():N}",
            SampleType = "Synthetic"
        };
        var result = new Result
        {
            BranchId = branchId,
            Sample = sample,
            SampleId = sample.Id,
            OrderItem = item,
            OrderItemId = item.Id,
            ValueText = "Initial synthetic value",
            UnitSnapshot = "synthetic-unit",
            Status = "Draft"
        };

        using var context = database.CreateContext();
        context.AddRange(patient, test, order, item, sample, result);
        await context.SaveChangesAsync();
        return (patient.Id, result.Id);
    }

    private static async Task<Guid> SeedInvoiceGraphAsync(AuditDatabase database, Guid branchId)
    {
        var patient = NewPatient(branchId, "Invoice");
        var order = new Order
        {
            BranchId = branchId,
            Patient = patient,
            PatientId = patient.Id,
            OrderNumber = $"O-{Guid.NewGuid():N}"
        };
        var invoice = new Invoice
        {
            BranchId = branchId,
            Order = order,
            OrderId = order.Id,
            InvoiceNumber = $"I-{Guid.NewGuid():N}",
            TotalPiaster = 10000,
            DiscountPiaster = 0,
            NetPiaster = 10000,
            PaidPiaster = 0,
            Status = "Open"
        };

        using var context = database.CreateContext();
        context.AddRange(patient, order, invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }

    private static async Task AssertHardDeleteRejectedAsync(
        AuditDatabase database,
        Func<LisDbContext, AuditedEntity> select)
    {
        using var context = database.CreateContext();
        context.Remove(select(context));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("hard-deleted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class AuditDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _anchor;

        private AuditDatabase(SqliteConnection anchor, DbContextOptions<LisDbContext> options)
        {
            _anchor = anchor;
            Options = options;
        }

        public DbContextOptions<LisDbContext> Options { get; }

        public static async Task<AuditDatabase> CreateAsync()
        {
            var databaseName = $"audit-{Guid.NewGuid():N}";
            var connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
            var anchor = new SqliteConnection(connectionString);
            await anchor.OpenAsync();

            var builder = new DbContextOptionsBuilder<LisDbContext>();
            builder.UseSqlite(connectionString);
            builder.UseBranchScope();

            var database = new AuditDatabase(anchor, builder.Options);
            using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public LisDbContext CreateContext() => new(Options);

        public async ValueTask DisposeAsync()
        {
            await _anchor.DisposeAsync();
        }
    }
}

import './App.css';
import { defaultLocale, messages } from './i18n/messages';

function App() {
  return (
    <main className="app">
      <h1>{messages[defaultLocale].appTitle}</h1>
    </main>
  );
}

export default App;

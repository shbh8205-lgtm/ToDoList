import React from 'react';
import { createRoot } from 'react-dom/client'; // שים לב לשינוי בנתיב הייבוא
import App from './App';

// מוצאים את האלמנט שבו האפליקציה צריכה להופיע
const container = document.getElementById('root');

// יוצרים את ה"שורש" (Root) של React
const root = createRoot(container);

// מרנדרים את האפליקציה בתוך השורש שיצרנו
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
import React, { useEffect, useState } from 'react';
import service from './service.js';
import Login from './Login'; 
function App() {
  const [newTodo, setNewTodo] = useState("");
  const [todos, setTodos] = useState([]);
  // --- הוספה: ניהול מצב התחברות ---
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem('token'));

  async function getTodos() {
    try {
      const todos = await service.getTasks();
      setTodos(todos);
    } catch (error) {
      console.error("נכשל בטעינת משימות", error);
      // אם קיבלנו שגיאת הרשאות, נוציא את המשתמש ללוגין
      if (error.response?.status === 401) {
        handleLogout();
      }
    }
  }

  async function createTodo(e) {
    e.preventDefault();
    if (!newTodo.trim()) return;
    await service.addTask(newTodo);
    setNewTodo("");
    await getTodos();
  }

  async function updateCompleted(todo, isComplete) {
    todo.isComplete=isComplete
    await service.setCompleted(todo.id, todo);
    await getTodos();
  }

  async function deleteTodo(id) {
    await service.deleteTask(id);
    await getTodos();
  }

  // --- הוספה: פונקציית התנתקות ---
  function handleLogout() {
    localStorage.removeItem('token');
    setIsLoggedIn(false);
    setTodos([]);
  }

  useEffect(() => {
    if (isLoggedIn) {
      getTodos();
    }
  }, [isLoggedIn]); // יתבצע בכל פעם שמצב ההתחברות משתנה

  // --- תנאי: אם לא מחובר, הצג מסך לוגין ---
  if (!isLoggedIn) {
    return <Login onLoginSuccess={() => setIsLoggedIn(true)} />;
  }

  return (
    <section className="todoapp">
  <header className="header">
    {/* קונטיינר עליון לכותרת וכפתור התנתקות */}
    <div style={{ 
      display: 'flex', 
      flexDirection: 'column', 
      alignItems: 'center', 
      padding: '20px',
      gap: '10px' 
    }}>
      <h1 style={{ 
        position: 'static', // דורס את ה-absolute מה-CSS החיצוני
        inset: 'auto', 
        marginTop: '0', 
        display: 'block',
        fontSize: '40px' // התאמת גודל למבנה החדש
      }}>
        {`${localStorage.getItem('userName') || 'User'}'s todos`}
      </h1>
      
      <button onClick={handleLogout} className="logout-btn" style={{ cursor: 'pointer' }}>
        התנתק
      </button>
    </div>

    {/* טופס הזנת משימה חדשה - מופיע רק פעם אחת */}
    <form onSubmit={createTodo}>
      <input 
        className="new-todo" 
        placeholder="Well, let's take on the day" 
        value={newTodo} 
        onChange={(e) => setNewTodo(e.target.value)} 
      />
    </form>
  </header>
  
  <section className="main" style={{ display: "block" }}>
    <ul className="todo-list">
      {todos.map(todo => (
        <li className={todo.isComplete ? "completed" : ""} key={todo.id}>
          <div className="view">
            <input 
              className="toggle" 
              type="checkbox" 
              checked={todo.isComplete} 
              onChange={(e) => updateCompleted(todo, e.target.checked)} 
            />
            <label>{todo.taskName}</label>
            <button className="destroy" onClick={() => deleteTodo(todo.id)}></button>
          </div>
        </li>
      ))}
    </ul>
  </section>
</section>
  );
}

export default App;

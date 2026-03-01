import React, { useState } from 'react';
import taskService from './service';

const Login = ({ onLoginSuccess }) => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            await taskService.login(username, password);
            onLoginSuccess(); // מעדכן את App שהתחברנו בהצלחה
        } catch (err) {
            alert("שגיאה בהתחברות: בדקי שם משתמש וסיסמה");
        }
    };

    return (
        <div style={{ maxWidth: '300px', margin: '50px auto', textAlign: 'center' }}>
            <h2>כניסה למערכת</h2>
            <form onSubmit={handleSubmit}>
                <input type="text" placeholder="שם משתמש" value={username} 
                       onChange={e => setUsername(e.target.value)} style={{display:'block', width:'100%', marginBottom:'10px'}} />
                <input type="password" placeholder="סיסמה" value={password} 
                       onChange={e => setPassword(e.target.value)} style={{display:'block', width:'100%', marginBottom:'10px'}} />
                <button type="submit" style={{width:'100%'}}>התחבר</button>
            </form>
        </div>
    );
};

export default Login;
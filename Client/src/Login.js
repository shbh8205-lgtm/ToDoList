import React, { useState } from 'react';
import taskService from './service';

const Login = ({ onLoginSuccess }) => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [isRegisterMode, setIsRegisterMode] = useState(false); // מצב הרשמה או כניסה

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            if (isRegisterMode) {
                // קריאה לפונקציית ההרשמה שהוספנו ל-service
                await taskService.register(username, password);      
                alert("נרשמת בהצלחה!");
            } else
                await taskService.login(username, password);
            onLoginSuccess();
            localStorage.setItem("userName", username);
        } catch (err) {
            const action = isRegisterMode ? "הרשמה" : "התחברות";
            alert(`שגיאה ב${action}: בדוק את הפרטים ונסה שוב`);
        }
    };

    return (
        <div style={{ maxWidth: '300px', margin: '50px auto', textAlign: 'center', fontFamily: 'Arial' }}>
            <h2>{isRegisterMode ? 'יצירת חשבון חדש' : 'כניסה למערכת'}</h2>

            <form onSubmit={handleSubmit}>
                <input
                    type="text"
                    placeholder="שם משתמש"
                    value={username}
                    onChange={e => setUsername(e.target.value)}
                    style={{ display: 'block', width: '100%', marginBottom: '10px', padding: '8px', boxSizing: 'border-box' }}
                    required
                />
                <input
                    type="password"
                    placeholder="סיסמה"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    style={{ display: 'block', width: '100%', marginBottom: '10px', padding: '8px', boxSizing: 'border-box' }}
                    required
                />
                <button type="submit" style={{ width: '100%', padding: '10px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}>
                    {isRegisterMode ? 'הירשם עכשיו' : 'התחבר'}
                </button>
            </form>

            <div style={{ marginTop: '15px' }}>
                <button
                    onClick={() => setIsRegisterMode(!isRegisterMode)}
                    style={{ background: 'none', border: 'none', color: '#007bff', cursor: 'pointer', textDecoration: 'underline' }}
                >
                    {isRegisterMode ? 'כבר יש לך חשבון? להתחברות' : 'אין לך חשבון? הירשם כאן'}
                </button>
            </div>
        </div>
    );
};

export default Login;

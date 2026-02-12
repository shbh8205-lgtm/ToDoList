import axios from 'axios';

// הגדרת כתובת ה-API כברירת מחדל לכל הקריאות
axios.defaults.baseURL = "http://localhost:5111";

// הגדרת interceptor לטיפול בשגיאות ב-Response
axios.interceptors.response.use(
  response => response, // אם התגובה תקינה, פשוט מחזירים אותה
  error => {
    // כאן אנחנו תופסים כל שגיאה שחוזרת מה-API (כמו 404, 500 וכו')
    console.error('API Error:', {
      message: error.message,
      status: error.response?.status,
      data: error.response?.data
    });
    return Promise.reject(error);
  }
);

const taskService = {
  // שליפת כל המשימות
  getTasks: async () => {
    const result = await axios.get("/items");
    return result.data;
  },

  // הוספת משימה חדשה - POST
  addTask: async (name) => {
    const result = await axios.post("/items", { name, isComplete: false });
    return result.data;
  },

  // עדכון מצב משימה (השלמה/ביטול) - PUT
  setCompleted: async (id, isComplete) => {
    const result = await axios.put(`/items/${id}`, { isComplete });
    return result.data;
  },

  // מחיקת משימה - DELETE
  deleteTask: async (id) => {
    const result = await axios.delete(`/items/${id}`);
    return result.data;
  }
};

export default taskService;
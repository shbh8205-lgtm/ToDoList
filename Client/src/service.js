import axios from 'axios';

axios.defaults.baseURL = process.env.REACT_APP_API_URL //|| "http://localhost:8080/";

// --- מנגנון הזרקת הטוקן לכל בקשה ---
axios.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axios.interceptors.response.use(
  response => response,
  error => {
    console.error('API Error:', {
      message: error.message,
      status: error.response?.status,
      data: error.response?.data
    });
    return Promise.reject(error);
  }
);

const taskService = {
  // --- הוספה: פונקציית התחברות ---
  login: async (userName, password) => {
    const result = await axios.post("/login", { userName, password });
    if (result.data.token) {
      localStorage.setItem('token', result.data.token); // שמירה בדפדפן
    }
    return result.data;
  },
  
  getTasks: async () => {
    try {
      const result = await axios.get("/items");

      // בדיקה: אם result.data קיים והוא מערך, נחזיר אותו. 
      // אם לא (למשל במקרה של 304 ריק או שגיאה), נחזיר מערך ריק [].
      return Array.isArray(result.data) ? result.data : [];

    } catch (error) {
      console.error("Error fetching tasks:", error);
      return []; // החזרת מערך ריק בשגיאה מונעת את קריסת ה-map
    }
  },

  // הוספת משימה חדשה - POST
  addTask: async (name) => {
    const result = await axios.post("/items", { name, isComplete: false });
    return result.data;
  },

  // עדכון מצב משימה (השלמה/ביטול) - PUT
  setCompleted: async (id, todo) => {

    const result = await axios.put(`/items/${id}`, todo);
    return result.data;
  },

  // מחיקת משימה - DELETE
  deleteTask: async (id) => {
    const result = await axios.delete(`/items/${id}`);
    return result.data;
  }
};

export default taskService;
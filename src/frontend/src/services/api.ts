import axios from 'axios';

let accessToken: string | null = null;
let logoutCallback: (() => void) | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

export const onLogout = (cb: () => void) => {
  logoutCallback = cb;
};

const api = axios.create({
  baseURL: 'http://localhost:5149/api', // Backend URL
  withCredentials: true, // Important for cookies
});

api.interceptors.request.use(
  (config) => {
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (!error.response) {
      error.message = 'Unable to connect to server. Please check your connection.';
      return Promise.reject(error);
    }

    // If 401 and not already retried
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        // Call refresh endpoint
        const response = await axios.post('http://localhost:5000/api/auth/refresh', {}, { withCredentials: true });
        const newAccessToken = response.data.accessToken;

        setAccessToken(newAccessToken);
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

        return api(originalRequest);
      } catch (refreshError) {
        // Redirect to login or handle session expiry
        setAccessToken(null);
        if (logoutCallback) logoutCallback();
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);

export default api;

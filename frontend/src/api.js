import axios from 'axios';

const API = axios.create({ baseURL: 'http://localhost:5140/api' });

const api = {
  getGroups: () => API.get('/group'),
  createGroup: (title) => API.post('/group', { title }),
  getGroup: (id) => API.get(`/group/${id}`),
  getMembers: (groupId) => API.get(`/group/${groupId}/members`),
  addMember: (groupId, name) => API.post(`/group/${groupId}/members`, { name }),
  removeMember: (groupId, memberId) => API.delete(`/group/${groupId}/members/${memberId}`),
  getTransactions: (groupId) => API.get(`/group/${groupId}/transactions`),
  createTransaction: (transaction) => API.post('/transaction', transaction),
  settleUp: (groupId, memberId) => API.post(`/group/${groupId}/settle/${memberId}`)
};

export default api;

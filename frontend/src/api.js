import axios from 'axios';

const apiClient = axios.create({ 
    baseURL: 'http://localhost:5140/api',
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
});

const groupService = {
    fetchAllGroups: () => apiClient.get('/group'),
    createNewGroup: (title) => apiClient.post('/group', { title }),
    fetchGroupById: (id) => apiClient.get(`/group/${id}`),
    fetchGroupMembers: (groupId) => apiClient.get(`/group/${groupId}/members`),
    addGroupMember: (groupId, name) => apiClient.post(`/group/${groupId}/members`, { name }),
    removeGroupMember: (groupId, memberId) => apiClient.delete(`/group/${groupId}/members/${memberId}`),
    fetchGroupTransactions: (groupId) => apiClient.get(`/group/${groupId}/transactions`),
    createNewTransaction: (transactionData) => apiClient.post('/transaction', transactionData),
    settleUserBalance: (groupId, memberId) => apiClient.post(`/group/${groupId}/settle/${memberId}`)
  };

export default groupService;

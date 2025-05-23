import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import groupService from '../api';

export default function GroupDetail() {
  const { id } = useParams();
  const [currentGroup, setCurrentGroup] = useState(null);
  const [groupMembers, setGroupMembers] = useState([]);
  const [groupTransactions, setGroupTransactions] = useState([]);
  const [memberName, setMemberName] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState(null);

  useEffect(() => {
    loadGroupData();
  }, [id]);

  const loadGroupData = async () => {
    try {
      const [groupResponse, membersResponse, transactionsResponse] = await Promise.all([
        groupService.fetchGroupById(id),
        groupService.fetchGroupMembers(id),
        groupService.fetchGroupTransactions(id),
      ]);

      setCurrentGroup(groupResponse.data);
      setGroupMembers(membersResponse.data);
      setGroupTransactions(transactionsResponse.data);
      setIsLoading(false);
    } catch (error) {
      setErrorMessage('Failed to load group data');
      setIsLoading(false);
    }
  };
    

  const handleAddMember = async () => {
    if (!memberName.trim()) return;
    
    try {
      await groupService.addGroupMember(id, memberName);
      setMemberName('');
      const response = await groupService.fetchGroupMembers(id);
      setGroupMembers(response.data);
    } catch (error) {
      const message = error.response?.data || 'failed to add member';
      alert(`Error: ${message}`);
    }
  };
  
  const handleRemoveMember = async (memberId) => {
    try {
      await groupService.removeGroupMember(id, memberId);
      const response = await groupService.fetchGroupMembers(id);
      setGroupMembers(response.data);
    } catch (error) {
      const message = error.response?.data || 'Failed to remove member';
      alert(`Error: ${message}`);
    }
  };
  
  const handleSettleBalance = async (memberId) => {
    try {
      await groupService.settleUserBalance(id, memberId);
      const response = await groupService.fetchGroupMembers(id);
      setGroupMembers(response.data);
    } catch (error) {
      alert('Failed to settle up. Please try again.');
    }
  };

  if (isLoading) return <div className="text-center py-8">Loading group details...</div>;
  if (errorMessage) return <div className="text-red-500 text-center py-8">{errorMessage}</div>;

  return (
    <div className="max-w-4xl mx-auto">
      <header className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">{currentGroup.title}</h1>
        <p className="text-gray-600">Manage your group expenses and settlements</p>
      </header>

      <div className="bg-white rounded-lg shadow-sm border p-6 mb-8">
        <h2 className="text-xl font-semibold text-gray-800 mb-4">Group Members</h2>
        
        <ul className="space-y-3 mb-6">
          {groupMembers.map((member) => (
            <li key={member.id} className="flex items-center justify-between py-2 px-4 bg-gray-50 rounded-lg">
              <div className="flex items-center space-x-3">
                <span className="font-medium text-gray-900">{member.name}&nbsp;</span>
                <span className={`text-sm font-semibold ${
                  member.balance > 0 ? 'text-red-600' : member.balance < 0 ? 'text-green-600' : 'text-gray-500'
                }`}>
                  Balance: ${Math.abs(member.balance).toFixed(2)} {member.balance > 0 ? 'owes' : member.balance < 0 ? 'is owed' : ''}
                </span>
              </div>
              <div className="flex space-x-2">
                {member.balance !== 0 && (
                  <button 
                    onClick={() => handleSettleBalance(member.id)} 
                    className="px-3 py-1 text-sm bg-green-100 text-green-700 rounded hover:bg-green-200 transition-colors"
                  >
                    Settle Up
                  </button>
                )}
                <button
                  onClick={() => handleRemoveMember(member.id)}
                  className="px-3 py-1 text-sm bg-red-100 text-red-700 rounded hover:bg-red-200 transition-colors"
                >
                  Remove
                </button>
              </div>
            </li>
          ))}
        </ul>

        <div className="flex space-x-3">
          <input
            value={memberName}
            onChange={(e) => setMemberName(e.target.value)}
            className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Enter member name"
          />
          <button 
            onClick={handleAddMember} 
            className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
          >
            Add Member
          </button>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow-sm border p-6 mb-6">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-semibold text-gray-800">Recent Transactions</h2>
          <Link 
            to={`/group/${id}/new-transaction`} 
            className="px-4 py-2 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition-colors"
          >
            + New Expense
          </Link>
        </div>

        {groupTransactions.length > 0 ? (
          <ul className="space-y-3">
            {groupTransactions.map((transaction) => (
              <li key={transaction.id} className="flex justify-between items-center py-3 px-4 bg-gray-50 rounded-lg">
                <div>
                  <span className="font-medium text-gray-900">{transaction.description}</span>
                  <span className="text-gray-600 ml-2">• Paid by {transaction.paidByName}</span>
                </div>
                <span className="font-semibold text-gray-900">${parseFloat(transaction.amount).toFixed(2)}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-gray-500 text-center py-8">No transactions yet. Add your first expense!</p>
        )}
      </div>
    </div>
  );
}
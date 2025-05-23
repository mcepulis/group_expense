import React, { useEffect, useState } from 'react';
import groupService from '../api';
import { Link } from 'react-router-dom';

function GroupList() {
  const [userGroups, setUserGroups] = useState([]);
  const [groupTitle, setGroupTitle] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    loadUserGroups();
  }, []);

  const loadUserGroups = async () => {
    try {
      const response = await groupService.fetchAllGroups();
      setUserGroups(response.data);
      setErrorMessage('');
    } catch (error) {
      setErrorMessage('Unable to load your groups');
    }
  };

  const createNewGroup = async () => {
    if (!groupTitle.trim()) return;
    
    setIsCreating(true);
    try {
      await groupService.createNewGroup(groupTitle);
      setGroupTitle('');
      await loadUserGroups();
      setErrorMessage('');
    } catch (error) {
      setErrorMessage('Failed to create group');
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto">
      <header className="text-center mb-8">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">Expense Groups</h1>
        <p className="text-gray-600">Track shared expenses with friends and family</p>
      </header>

      <div className="bg-white rounded-lg shadow-sm border p-6 mb-8">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Create New Group</h2>
        <div className="flex space-x-3">
          <input
            className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            value={groupTitle}
            onChange={e => setGroupTitle(e.target.value)}
            placeholder="Enter group name (e.g., 'Weekend Trip', 'Roommates')"
          />
          <button
            className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            onClick={createNewGroup}
            disabled={!groupTitle.trim() || isCreating}
          >
            {isCreating ? 'Creating...' : 'Create Group'}
          </button>
        </div>
        {errorMessage && <p className="text-red-500 text-sm mt-2">{errorMessage}</p>}
      </div>

      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Your Groups</h2>
        {userGroups.length > 0 ? (
          <ul className="space-y-3">
            {userGroups.map(group => (
              <li key={group.id} className="flex items-center justify-between py-3 px-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                <Link to={`/group/${group.id}`} className="flex-1 text-indigo-600 hover:text-indigo-800 font-medium">
                  {group.title}
                </Link>
                {group.balance !== undefined && (
                  <span className={`text-sm font-semibold ${
                    group.balance < 0 ? 'text-green-600' : group.balance > 0 ? 'text-red-600' : 'text-gray-500'
                  }`}>
                    {group.balance === 0 ? 'Settled' : `$${Math.abs(group.balance).toFixed(2)} ${group.balance < 0 ? 'you are owed' : 'you owe'}`}
                  </span>
                )}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-gray-500 text-center py-8">No groups yet. Create your first group to get started!</p>
        )}
      </div>
    </div>
  );
}

export default GroupList;
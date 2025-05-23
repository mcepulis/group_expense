import React, { useEffect, useState } from 'react';
import api from '../api';
import { Link } from 'react-router-dom';

function GroupList() {
  const [groups, setGroups] = useState([]);
  const [title, setTitle] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    fetchGroups();
  }, []);

  const fetchGroups = async () => {
    try {
      const { data } = await api.getGroups();
      setGroups(data);
      setError('');
    } catch (err) {
      setError('Failed to fetch groups');
    }
  };

  const handleCreate = async () => {
    if (!title) return;
    try {
      await api.createGroup(title);
      setTitle('');
      fetchGroups();
      setError('');
    } catch (err) {
      setError('Failed to create group');
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-4">Your Groups</h1>
      <div className="mb-4">
        <input
          className="border p-2 mr-2"
          value={title}
          onChange={e => setTitle(e.target.value)}
          placeholder="Group Title"
        />
        <button
          className="bg-blue-500 text-white px-4 py-2"
          onClick={handleCreate}
          disabled={!title.trim()}
        >
          Create
        </button>
      </div>
      {error && <p className="text-red-600 mb-2">{error}</p>}
      <ul>
      {groups.map(group => (
        <li key={group.id} className="mb-2">
          <Link to={`/group/${group.id}`} className="text-blue-600 underline">
            {group.title}
          </Link>
          {group.balance !== undefined && (
            <span className="ml-2">
              — Balance: <span className={group.balance < 0 ? 'text-green-600' : group.balance > 0 ? 'text-red-600' : ''}>
                {group.balance.toFixed(2)}
              </span>
            </span>
          )}
        </li>
      ))}
      </ul>
    </div>
  );
}

export default GroupList;

import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';

export default function Groups() {
  const [groups, setGroups] = useState([]);
  const [newTitle, setNewTitle] = useState('');

  useEffect(() => {
    const fetchGroups = async () => {
      const res = await api.get('/group');
      setGroups(res.data);
    };
    fetchGroups();
  }, []);

  const createGroup = async () => {
    if (!newTitle) return;
    await api.post('/group', { title: newTitle });
    setNewTitle('');
    const res = await api.get('/group');
    setGroups(res.data);
  };

  return (
    <div className="p-4">
      <h2 className="text-2xl font-bold mb-4">Groups</h2>
      <ul className="list-disc pl-6 mb-4">
        {groups.map(g => (
          <li key={g.id}>
            <Link to={`/group/${g.id}`} className="text-blue-600 underline">{g.title}</Link> – Balance: {g.balance}
          </li>
        ))}
      </ul>
      <input
        value={newTitle}
        onChange={(e) => setNewTitle(e.target.value)}
        className="border px-2 py-1 mr-2"
        placeholder="New group title"
      />
      <button onClick={createGroup} className="bg-blue-500 text-white px-3 py-1">Create Group</button>
    </div>
  );
}

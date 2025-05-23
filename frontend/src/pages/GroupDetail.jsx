import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../api';

export default function GroupDetail() {
  const { id } = useParams();
  const [group, setGroup] = useState(null);
  const [members, setMembers] = useState([]);
  const [transactions, setTransactions] = useState([]);
  const [newMemberName, setNewMemberName] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [groupRes, membersRes, transactionsRes] = await Promise.all([
          api.getGroup(id),
          api.getMembers(id),
          api.getTransactions(id)
        ]);
        setGroup(groupRes.data);
        setMembers(membersRes.data);
        setTransactions(transactionsRes.data);
        setLoading(false);
      } catch (err) {
        console.error(err);
        setError('Failed to load group data');
        setLoading(false);
      }
    };
    fetchData();
  }, [id]);

  const addMember = async () => {
    if (!newMemberName) return;
    
    try {
      console.log('Attempting to add member:', newMemberName);
      console.log('Group ID:', id);
      
      const response = await api.addMember(id, newMemberName);
      console.log('Success response:', response);
      
      setNewMemberName('');
      const res = await api.getMembers(id);
      setMembers(res.data);
    } catch (error) {
      console.error('Full error object:', error);
      console.error('Error response:', error.response?.data);
      console.error('Error status:', error.response?.status);
      console.error('Error message:', error.message);
      
      const errorMessage = error.response?.data || error.message;
      alert(`Failed to add member: ${errorMessage}`);
    }
  };
  
  const removeMember = async (memberId) => {
    try {
      await api.removeMember(id, memberId);
      const res = await api.getMembers(id);
      setMembers(res.data);
    } catch (error) {
      console.error('Failed to remove member:', error);
      const message = error.response?.data || error.message;
      alert(`Failed to remove member: ${message}`);
    }
  };
  
  
  const settleUp = async (memberId) => {
    try {
      await api.settleUp(id, memberId);
      const res = await api.getMembers(id);
      setMembers(res.data);
    } catch (error) {
      console.error('Failed to settle up:', error);
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="text-red-600">{error}</div>;

  return (
    <div className="p-4">
      <h2 className="text-2xl font-bold mb-4">{group.title}</h2>
      <div className="mb-6">
        <h3 className="text-xl font-semibold">Members</h3>
        <ul className="list-disc pl-6">
          {members.map((m) => (
            <li key={m.id}>
              {m.name} — Balance: {m.balance.toFixed(2)}
              {m.balance !== 0 && (
                <button onClick={() => settleUp(m.id)} className="ml-2 text-blue-600">Settle</button>
              )}
              <button
                onClick={() => removeMember(m.id)}
                className="ml-2 text-red-500"
              >
                Remove
              </button>
            </li>
          ))}
        </ul>
        <input
          value={newMemberName}
          onChange={(e) => setNewMemberName(e.target.value)}
          className="border px-2 py-1 mr-2"
          placeholder="New member name"
        />
        <button onClick={addMember} className="bg-blue-500 text-white px-3 py-1">Add</button>
      </div>

      <div className="mb-4">
        <Link to={`/group/${id}/new-transaction`} className="text-green-600 font-semibold">+ Add Transaction</Link>
      </div>

      <div>
        <h3 className="text-xl font-semibold mb-2">Transactions</h3>
        <ul className="list-disc pl-6">
          {transactions.map((t) => (
            <li key={t.id}>{t.description} – Paid by {t.paidByName} – {parseFloat(t.amount).toFixed(2)}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}

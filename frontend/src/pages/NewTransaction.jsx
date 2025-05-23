import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import api from '../api';

export default function NewTransaction() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [members, setMembers] = useState([]);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [paidBy, setPaidBy] = useState('');
  const [splitMethod, setSplitMethod] = useState('equally');
  const [splits, setSplits] = useState({});

  useEffect(() => {
    const fetchMembers = async () => {
      const res = await api.getMembers(id);
      setMembers(res.data);
    };
    fetchMembers();
  }, [id]);

  const handleSplitChange = (memberId, value) => {
    setSplits({ ...splits, [memberId]: value });
  };

const submit = async () => {
  if (!description || !amount || !paidBy) {
    alert('Please fill in all required fields');
    return;
  }

  const amt = parseFloat(amount);
  if (isNaN(amt) || amt <= 0) {
    alert('Please enter a valid amount');
    return;
  }

  let splitTypeValue;
  switch (splitMethod) {
    case 'equally':
      splitTypeValue = 0; 
      break;
    case 'percentage':
      splitTypeValue = 1; 
      break;
    case 'dynamic':
      splitTypeValue = 2; 
      break;
    default:
      splitTypeValue = 0;
  }

  if (splitMethod !== 'equally') {
    const splitValues = Object.values(splits);
    const total = splitValues.reduce((sum, val) => sum + parseFloat(val || 0), 0);

    if (splitMethod === 'percentage') {
      if (Math.abs(total - 100) > 0.01) {
        alert('Total percentage must equal 100%');
        return;
      }
    } else if (splitMethod === 'dynamic') {
      if (Math.abs(total - amt) > 0.01) {
        alert(`Total split (${total.toFixed(2)}) must equal the total amount (${amt.toFixed(2)})`);
        return;
      }
    }
  }

  const payload = {
    groupId: parseInt(id),
    description,
    totalAmount: amt,
    payerId: parseInt(paidBy),
    splitType: splitTypeValue, 
    splits: Object.entries(splits).map(([memberId, value]) => ({
      memberId: parseInt(memberId),
      value: parseFloat(value)
    }))
  };

  console.log('=== Transaction Debug ===');
  console.log('Payload being sent:', payload);

  try {
    await api.createTransaction(payload);
    navigate(`/group/${id}`);
  } catch (error) {
    console.log('=== Transaction Error ===');
    console.error('Full error object:', error);
    console.error('Error response:', error.response?.data);
    console.error('Error status:', error.response?.status);
    console.error('Error message:', error.message);
    console.error('Validation errors:', error.response?.data?.errors);
    
    const errorMessage = error.response?.data?.title || error.response?.data || error.message;
    alert('Failed to create transaction: ' + errorMessage);
  }
};

  return (
    <div className="p-4">
      <h2 className="text-xl font-bold mb-4">New Transaction</h2>
      <input
        className="block mb-2 border px-2 py-1 w-full"
        placeholder="Description"
        value={description}
        onChange={e => setDescription(e.target.value)}
      />
      <input
        className="block mb-2 border px-2 py-1 w-full"
        placeholder="Amount"
        type="number"
        value={amount}
        onChange={e => setAmount(e.target.value)}
      />
      <select
        className="block mb-2 border px-2 py-1 w-full"
        value={paidBy}
        onChange={e => setPaidBy(e.target.value)}
      >
        <option value="">Paid By</option>
        {members.map(m => (
          <option key={m.id} value={m.id}>{m.name}</option>
        ))}
      </select>

      <select
        className="block mb-4 border px-2 py-1 w-full"
        value={splitMethod}
        onChange={e => setSplitMethod(e.target.value)}
      >
        <option value="equally">Equally</option>
        <option value="percentage">By Percentage</option>
        <option value="dynamic">Custom Amounts</option>
      </select>

      {(splitMethod !== 'equally') && (
        <div>
          {members.map(m => (
            <div key={m.id} className="mb-2">
              <label>{m.name}</label>
              <input
                type="number"
                className="border ml-2 px-2 py-1"
                placeholder={splitMethod === 'percentage' ? 'Percent' : 'Amount'}
                onChange={e => handleSplitChange(m.id, e.target.value)}
              />
            </div>
          ))}
        </div>
      )}

      <button
        onClick={submit}
        className="bg-green-500 text-white px-4 py-2"
      >
        Submit
      </button>
    </div>
  );
}

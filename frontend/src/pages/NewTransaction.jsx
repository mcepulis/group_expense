import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import groupService from '../api';

export default function NewTransaction() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [availableMembers, setAvailableMembers] = useState([]);
  const [expenseDescription, setExpenseDescription] = useState('');
  const [totalAmount, setTotalAmount] = useState('');
  const [payingMember, setPayingMember] = useState('');
  const [divisionMethod, setDivisionMethod] = useState('equally');
  const [memberSplits, setMemberSplits] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    loadGroupMembers();
  }, [id]);

  const loadGroupMembers = async () => {
    try {
      const response = await groupService.fetchGroupMembers(id);
      setAvailableMembers(response.data);
    } catch (error) {
      alert('Unable to load group members');
    }
  };

  const updateMemberSplit = (memberId, value) => {
    setMemberSplits({ ...memberSplits, [memberId]: value });
  };

  const validateAndSubmit = async () => {
    if (!expenseDescription.trim() || !totalAmount || !payingMember) {
      alert('Please fill in all required fields');
      return;
    }

    const amount = parseFloat(totalAmount);
    if (isNaN(amount) || amount <= 0) {
      alert('Please enter a valid amount');
      return;
    }

    let splitTypeValue;
    switch (divisionMethod) {
      case 'equally':
        splitTypeValue = 0; 
        break;
      case 'percentage':
        splitTypeValue = 1; 
        break;
      case 'custom':
        splitTypeValue = 2; 
        break;
      default:
        splitTypeValue = 0;
    }

    if (divisionMethod !== 'equally') {
      const splitValues = Object.values(memberSplits);
      const total = splitValues.reduce((sum, val) => sum + parseFloat(val || 0), 0);

      if (divisionMethod === 'percentage' && Math.abs(total - 100) > 0.01) {
        alert('Total percentage must equal 100%');
        return;
      } else if (divisionMethod === 'custom' && Math.abs(total - amount) > 0.01) {
        alert(`Total split amounts (${total.toFixed(2)}) must equal the expense amount (${amount.toFixed(2)})`);
        return;
      }
    }

    const transactionPayload = {
      groupId: parseInt(id),
      description: expenseDescription,
      totalAmount: amount,
      payerId: parseInt(payingMember),
      splitType: splitTypeValue,
      splits: Object.entries(memberSplits).map(([memberId, value]) => ({
        memberId: parseInt(memberId),
        value: parseFloat(value)
      }))
    };

    setIsSubmitting(true);
    try {
      await groupService.createNewTransaction(transactionPayload);
      navigate(`/group/${id}`);
    } catch (error) {
      const errorMessage = error.response?.data?.title || error.response?.data || 'Failed to create transaction';
      alert('Error: ' + errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto">
      <header className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Add New Expense</h1>
        <p className="text-gray-600">Record a shared expense for your group</p>
      </header>

      <div className="bg-white rounded-lg shadow-sm border p-6">
        <div className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">What was this expense for?</label>
            <input
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="e.g., Dinner at restaurant, Gas for road trip"
              value={expenseDescription}
              onChange={e => setExpenseDescription(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Total Amount</label>
            <input
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="0.00"
              type="number"
              step="0.01"
              value={totalAmount}
              onChange={e => setTotalAmount(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Who paid for this?</label>
            <select
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              value={payingMember}
              onChange={e => setPayingMember(e.target.value)}
            >
              <option value="">Select the person who paid</option>
              {availableMembers.map(member => (
                <option key={member.id} value={member.id}>{member.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">How should this be split?</label>
            <select
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              value={divisionMethod}
              onChange={e => setDivisionMethod(e.target.value)}
            >
              <option value="equally">Split equally among all members</option>
              <option value="percentage">Split by percentage</option>
              <option value="custom">Custom amounts for each person</option>
            </select>
          </div>

          {divisionMethod !== 'equally' && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-3">
                {divisionMethod === 'percentage' ? 'Set percentage for each member' : 'Set amount for each member'}
              </h3>
              <div className="space-y-3">
                {availableMembers.map(member => (
                  <div key={member.id} className="flex items-center justify-between">
                    <label className="text-sm text-gray-600">{member.name}</label>
                    <input
                      type="number"
                      step={divisionMethod === 'percentage' ? '1' : '0.01'}
                      className="w-24 px-3 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                      placeholder={divisionMethod === 'percentage' ? '%' : '$'}
                      onChange={e => updateMemberSplit(member.id, e.target.value)}
                    />
                  </div>
                ))}
              </div>
            </div>
          )}

          <button
            onClick={validateAndSubmit}
            disabled={isSubmitting}
            className="w-full py-3 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
          >
            {isSubmitting ? 'Adding Expense...' : 'Add Expense'}
          </button>
        </div>
      </div>
    </div>
  );
}
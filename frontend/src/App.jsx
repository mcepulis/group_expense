import React from 'react';
import { Routes, Route, Link } from 'react-router-dom';
import GroupList from './pages/GroupList';
import GroupDetail from './pages/GroupDetail';
import NewTransaction from './pages/NewTransaction';

function App() {
  return (
    <div className="min-h-screen bg-gray-100 p-4">
      <nav className="mb-4">
        <Link to="/" className="text-blue-500 font-bold">Home</Link>
      </nav>
      <Routes>
        <Route path="/" element={<GroupList />} />
        <Route path="/group/:id" element={<GroupDetail />} />
        <Route path="/group/:id/new-transaction" element={<NewTransaction />} />
      </Routes>
    </div>
  );
}

export default App;

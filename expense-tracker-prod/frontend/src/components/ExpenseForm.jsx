import { useState } from "react";

const initialState = {
  amount: "",
  category: "",
  description: "",
  date: "",
};

export default function ExpenseForm({ onSubmit, loading }) {
  const [form, setForm] = useState(initialState);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    await onSubmit({
      ...form,
      amount: Number(form.amount).toFixed(2),
    });
    setForm(initialState);
  };

  return (
    <form className="card" onSubmit={handleSubmit}>
      <h2>Add Expense</h2>
      <div className="grid">
        <input
          name="amount"
          type="number"
          step="0.01"
          min="0.01"
          required
          value={form.amount}
          onChange={handleChange}
          placeholder="Amount"
        />
        <input name="category" required value={form.category} onChange={handleChange} placeholder="Category" />
        <input
          name="description"
          required
          value={form.description}
          onChange={handleChange}
          placeholder="Description"
        />
        <input name="date" required type="date" value={form.date} onChange={handleChange} />
      </div>
      <button disabled={loading} type="submit">
        {loading ? "Saving..." : "Save Expense"}
      </button>
    </form>
  );
}

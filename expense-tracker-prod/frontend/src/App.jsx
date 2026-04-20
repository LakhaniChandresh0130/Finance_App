import { useEffect, useMemo, useState } from "react";

import { createExpense, fetchExpenses } from "./api/client";
import ExpenseForm from "./components/ExpenseForm";
import ExpenseList from "./components/ExpenseList";

const PAGE_SIZE = 20;

export default function App() {
  const [categoryFilter, setCategoryFilter] = useState("");
  const [page, setPage] = useState(1);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalAmount, setTotalAmount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [expenses, setExpenses] = useState([]);

  const hasMore = expenses.length < totalRecords;

  const reload = async (targetPage, replace = false) => {
    setLoading(true);
    setError("");
    try {
      const result = await fetchExpenses({
        category: categoryFilter,
        page: targetPage,
        pageSize: PAGE_SIZE,
      });
      setTotalAmount(result.total_amount);
      setTotalRecords(result.total_records);
      setExpenses((current) => (replace ? result.data : [...current, ...result.data]));
    } catch (e) {
      setError(e.message ?? "Request failed");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
    void reload(1, true);
  }, [categoryFilter]);

  const loadMore = async () => {
    const nextPage = page + 1;
    setPage(nextPage);
    await reload(nextPage, false);
  };

  const submitExpense = async (payload) => {
    setLoading(true);
    setError("");
    try {
      await createExpense(payload);
      setPage(1);
      await reload(1, true);
    } catch (e) {
      setError(e.message ?? "Failed to create expense");
    } finally {
      setLoading(false);
    }
  };

  const categoryOptions = useMemo(() => {
    const options = new Set(expenses.map((x) => x.category));
    return Array.from(options.values()).sort();
  }, [expenses]);

  return (
    <main className="container">
      <h1>Expense Tracker</h1>
      {error ? <p className="error">{error}</p> : null}
      <ExpenseForm onSubmit={submitExpense} loading={loading} />
      <section className="card controls">
        <label htmlFor="category">Filter by category</label>
        <select id="category" value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
          <option value="">All</option>
          {categoryOptions.map((category) => (
            <option key={category} value={category}>
              {category}
            </option>
          ))}
        </select>
        <p>Sorting: newest first</p>
      </section>
      <ExpenseList
        expenses={expenses}
        totalAmount={totalAmount}
        loading={loading}
        hasMore={hasMore}
        onLoadMore={loadMore}
      />
    </main>
  );
}

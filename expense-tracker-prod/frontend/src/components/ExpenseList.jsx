export default function ExpenseList({ expenses, totalAmount, loading, hasMore, onLoadMore }) {
  return (
    <section className="card">
      <div className="list-header">
        <h2>Expenses</h2>
        <strong>Total: ₹{Number(totalAmount).toFixed(2)}</strong>
      </div>
      {loading && expenses.length === 0 ? <p>Loading...</p> : null}
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Category</th>
            <th>Description</th>
            <th>Amount</th>
          </tr>
        </thead>
        <tbody>
          {expenses.map((expense) => (
            <tr key={expense.id}>
              <td>{expense.date}</td>
              <td>{expense.category}</td>
              <td>{expense.description}</td>
              <td>₹{Number(expense.amount).toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {hasMore ? (
        <button className="load-more" onClick={onLoadMore} disabled={loading}>
          {loading ? "Loading..." : "Load More"}
        </button>
      ) : null}
    </section>
  );
}

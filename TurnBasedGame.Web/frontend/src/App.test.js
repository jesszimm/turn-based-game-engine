import { render, screen } from '@testing-library/react';
import App from './App';

test('renders the game title', () => {
  render(<App />);
  const title = screen.getByText(/tactical squares/i);
  expect(title).toBeInTheDocument();
});

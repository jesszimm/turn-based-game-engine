export function getValidMoves(selectedUnit, units, boardSize) {
  if (!selectedUnit) return new Set();

  const occupied = new Set(units.map((unit) => `${unit.x},${unit.y}`));
  const validMoves = new Set();

  for (let y = 0; y < boardSize; y += 1) {
    for (let x = 0; x < boardSize; x += 1) {
      const distance = Math.abs(selectedUnit.x - x) + Math.abs(selectedUnit.y - y);
      if (distance === 0 || distance > selectedUnit.movementRange) continue;
      if (!occupied.has(`${x},${y}`)) validMoves.add(`${x},${y}`);
    }
  }

  return validMoves;
}

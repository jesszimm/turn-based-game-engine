const API_URL = process.env.REACT_APP_API_URL;

async function requestJson(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, options);
  return response;
}

export async function createGame(payload) {
  const options = { method: 'POST' };
  if (payload) {
    options.headers = { 'Content-Type': 'application/json' };
    options.body = JSON.stringify(payload);
  }
  return requestJson('/api/game/create', options);
}

export async function getGame(gameId) {
  return requestJson(`/api/game/${gameId}`);
}

export async function moveUnit(gameId, payload) {
  return requestJson(`/api/game/${gameId}/move`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

export async function attackUnit(gameId, payload) {
  return requestJson(`/api/game/${gameId}/attack`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

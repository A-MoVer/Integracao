//directus.js
const API_URL = '/api';  

export async function loginDirectus(email, password) {
  const response = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  const data = await response.json();
  console.log('Resposta do Directus:', data);  // ajuda brutal
  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro no login');
  }

  return data.data.access_token;
}

export async function getPublicacoes(token) {
  const response = await fetch(`${API_URL}/items/Publicacao`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar publicações');
  }

  return data.data;
}

export async function logoutDirectus(token) {
  // mesmo que falhe (ex.: token expirado) não queremos bloquear o UI
  try {
    await fetch(`${API_URL}/auth/logout`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch (_) {}
}
export async function getUserInfo(token) {
  const response = await fetch(`${API_URL}/users/me?fields=*,role.*`, {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  const data = await response.json();
  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar dados do usuário');
  }
  return data.data;
}
export async function getEquipasComMembros(token) {
  const response = await fetch(`${API_URL}/items/Equipa?fields=id,Nome,Logo`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const data = await response.json();
  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar equipas');
  }

  console.log("Dados das equipas recebidos:", data.data);

  return data.data.map(equipa => {
    console.log(`Equipa: ${equipa.Nome}, Logo recebido: ${equipa.Logo}`);

    return {
      ...equipa,
      Logo: equipa.Logo || 'HelpOutline' 
    };
  });
}
export async function getEquipaDetalhes(equipaId, token) {
  const response = await fetch(
    `${API_URL}/items/Equipa/${equipaId}?fields=*,membros.*,membros.Foto.*`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );

  const data = await response.json();
  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar equipa');
  }
  return data.data; 
}

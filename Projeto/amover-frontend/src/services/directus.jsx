// src/services/directus.js
const API_URL = '/api';

// -----------------------------
// 🔐 Autenticação
// -----------------------------
export async function loginDirectus(email, password) {
  const response = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  const data = await response.json();
  if (!response.ok) throw new Error(data?.errors?.[0]?.message || 'Erro no login');
  return data.data.access_token;
}

export async function logoutDirectus(token) {
  try {
    await fetch(`${API_URL}/auth/logout`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch (_) {}
}

// -----------------------------
// 📋 utilizador completo
// -----------------------------


export async function getUserInfo(token) {
  const response = await fetch(`/api/users/me?fields=id,email,first_name,last_name,avatar,role.id,role.name`, {
    headers: { Authorization: `Bearer ${token}` }
  });

  const result = await response.json();
  const user = result.data;

  if (!response.ok || !user?.id) throw new Error('Falha ao obter utilizador');

  // Se os campos são textos simples (e não relações), não ponhas ".name"
  const perfilRes = await fetch(`/api/items/persons?filter[user][_eq]=${user.id}&fields=*,person_type,academic_level,team.id,team.name,orientador.id,orientador.user.first_name,orientador.user.last_name,orientador.user.avatar`, {
    headers: { Authorization: `Bearer ${token}` }
  });


  

  const perfilData = await perfilRes.json();
  const person = perfilData.data?.[0] || {};

  const coorientadoresRes = await fetch(`/api/items/persons_coorientadores?filter[person_id][_eq]=${person.id}&fields=orientador_id.user.first_name,orientador_id.user.last_name,orientador_id.user.avatar`, {
  headers: { Authorization: `Bearer ${token}` }
});

  const coorientadoresData = await coorientadoresRes.json();
  const coorientadores = (coorientadoresData.data || []).map(entry => {
    const u = entry.orientador_id?.user;
    return u ? {
      nome: `${u.first_name} ${u.last_name}`,
      foto: u.avatar || null
    } : null;
  }).filter(Boolean);



  return {
    id: user.id,
    email: user.email || '',
    first_name: user.first_name || '',
    last_name: user.last_name || '',
    avatar: user.avatar || '',
    role: user.role || null,

    person_id : person?.id   || null,
    team_id   : person?.team?.id || null,
    perfil: {
      tipo: person?.person_type || 'Não definido',
      nivel_academico: person?.academic_level || 'Não definido',
      equipa: person?.team?.name || 'Não atribuída'
    },
    orientador: person?.orientador?.user
      ? {
          nome: `${person.orientador.user.first_name} ${person.orientador.user.last_name}`,
          foto: person.orientador.user.avatar || null
        }
      : null,
    coorientadores: coorientadores

  };
}





export async function getPublicacoes(token) {
  // --------------------- LOG REQUEST ---------------------
  const url =
    `${API_URL}/items/publications?` +
    'fields=id,title,abstract,status,published_on,doi_or_url,' +
    'file.directus_files_id.id,' +
    'file.directus_files_id.filename_download,' +
    'equipa_id.id,equipa_id.name,' +
    'Autores.id,Autores.user.id,Autores.user.first_name,Autores.user.last_name,Autores.user.avatar' +
    '&sort[]=-published_on';

  console.groupCollapsed('%c[getPublicacoes] ↗ REQ', 'color:#0984e3;font-weight:bold');
  console.log('Endpoint  :', url);
  console.log('Método    : GET');
  console.log('Token     :', token?.slice(0, 10) + '…');
  console.groupEnd();

  // --------------------- FETCH ----------------------------
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${token}` }
  });

  // --------------------- LOG RESPONSE ---------------------
  const json = await response.json();

  console.groupCollapsed(
    `%c[getPublicacoes] ↙ RES (${response.status} ${response.statusText})`,
    `color:${response.ok ? '#00b894' : '#d63031'};font-weight:bold`
  );
  console.log('JSON bruto:', json);
  if (Array.isArray(json?.errors)) {
    json.errors.forEach((err, i) => {
      console.group(`Erro[${i}] : ${err.message}`);
      console.table(err.extensions || {});
      console.groupEnd();
    });
  }
  console.groupEnd();

  if (!response.ok) {
    throw new Error(json?.errors?.[0]?.message || 'Erro ao buscar publicações');
  }

  // --------------------- MAP/SHAPE ------------------------
  return json.data.map((p) => {
    // Se “Autores” for uma lista, usa o primeiro autor
    const primeiroAutor = Array.isArray(p.Autores) ? p.Autores[0] : p.Autores;
    const autoresArray = Array.isArray(p.Autores)
      ? p.Autores
      : p.Autores ? [p.Autores] : [];

    const nomeAutor = [
      primeiroAutor?.user?.first_name,
      primeiroAutor?.user?.last_name
    ]
      .filter(Boolean)
      .join(' ') || 'Autor desconhecido';

    // Desembrulha status se for array e normaliza
    const rawStatusField = Array.isArray(p.status) ? p.status[0] : p.status;
    const statusNormalized = (rawStatusField || '').toLowerCase();

     const filePivot = Array.isArray(p.file) ? p.file[0] : p.file;

    // constrói o objeto ficheiro ou null
    const ficheiro = filePivot?.directus_files_id
      ? {
          id:   filePivot.directus_files_id.id,
          nome: filePivot.directus_files_id.filename_download,
          url:  `${API_URL}/assets/${filePivot.directus_files_id.id}`
        }
      : null;

    return {
      id      : p.id,
      titulo  : p.title,
      conteudo: p.abstract,
      status  : statusNormalized,
      autor   : nomeAutor,
      Autores : autoresArray,
      links   : p.doi_or_url ? p.doi_or_url.split('; ').filter(Boolean) : [],
      ficheiro,
      equipa  : { id: p.equipa_id?.id, nome: p.equipa_id?.name },
      data    : p.published_on
    };
  });
}

// -----------------------------
// 📚 Publicações
// -----------------------------


export async function createPublicacao(pub, token) {
  // monta o payload base
  const body = {
    title        : pub.title?.trim(),
    abstract     : pub.abstract?.trim(),
    status       : pub.status,
    equipa_id    : pub.equipa_id || null,
    Autores      : pub.authors?.[0] || null,
    doi_or_url   : (pub.doi_or_url || []).join('; '),
    published_on : pub.published_on || null
  };

  // 1) ONE-TO-ONE: caso use fileId
  if (pub.fileId) {
    body.file = pub.fileId;
  }
  // 2) M-N múltiplos: caso use array pub.files
  else if (Array.isArray(pub.files) && pub.files.length) {
    body.file = {
      create: pub.files.map(f => ({ directus_files_id: f.directus_files_id }))
    };
  }

  console.groupCollapsed('%c[createPublicacao] ↗ REQ', 'color:#0984e3;font-weight:bold');
  console.log('Endpoint  :', '/api/items/publications');
  console.log('Método    : POST');
  console.log('Payload   :', JSON.stringify(body, null, 2));
  console.groupEnd();

  const response = await fetch(`${API_URL}/items/publications`, {
    method : 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization : `Bearer ${token}`
    },
    body: JSON.stringify(body)
  });

  const resJson = await response.json();
  console.groupCollapsed(
    `%c[createPublicacao] ↙ RES (${response.status} ${response.statusText})`,
    `color:${response.ok ? '#00b894' : '#d63031'};font-weight:bold`
  );
  console.log('Response JSON:', resJson);
  console.groupEnd();

  if (!response.ok) {
    const msg = resJson?.errors?.[0]?.message || `${response.status} ${response.statusText}`;
    throw new Error(msg);
  }

  return resJson.data;
}


// -----------------------------
// 🧑‍🤝‍🧑 Equipas
// -----------------------------
export async function getEquipasComMembros(token) {
  const response = await fetch(
    `${API_URL}/items/teams?fields=id,name,area,photo,membros.id&populate[membros][populate][person_id][populate]=user`,
    {
      headers: { Authorization: `Bearer ${token}` }
    }
  );

  const data = await response.json();
  if (!response.ok)
    throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar equipas');

  return data.data.map((e) => ({
    id: e.id,
    Nome: e.name,
    Descricao: e.area,
    Logo: e.photo || 'HelpOutline',
    membros: (e.membros || []).map((bolsa) => {
      const user = bolsa?.person_id?.user || {};
      return {
        id: user.id || null,
        email: user.email || '',
        nome: [user.first_name, user.last_name].filter(Boolean).join(' ') || 'Sem nome',
      };
    })
  }));
}



export async function getEquipasPublicas() {
  console.log('🔍 Buscando equipas públicas...');

  const response = await fetch(
    `${API_URL}/items/teams?fields=id,name,area,photo,leader.id,leader.first_name,leader.last_name,leader.email,membros.id,membros.person_id.id,membros.person_id.user.first_name,membros.person_id.user.last_name,membros.person_id.user.email,membros.person_id.user.avatar,membros.person_id.person_type,membros.person_id.academic_level&populate[leader]=true&populate[membros][populate]=person_id.user`
  );

  console.log('📝 Resposta da API:', response);
  const data = await response.json();
  console.log('📦 Dados recebidos da API:', data);

  if (!response.ok) {
    console.error('❌ Erro ao carregar as equipas públicas:', data?.errors?.[0]?.message);
    throw new Error(data?.errors?.[0]?.message || 'Erro ao carregar equipas públicas');
  }

  const equipasComLideres = data.data.map((e) => {
    console.log('🧩 Equipa recebida:', e);

    const leader = e.leader
      ? `Líder: ${e.leader.first_name} ${e.leader.last_name} email ${e.leader.email}`
      : 'Sem líder definido';
    console.log(leader);

    return {
      id: e.id,
      Nome: e.name,
      Descricao: e.area,
      Logo: e.photo || null,
      leader: e.leader || null,
      membros: (e.membros || []).map((bolsa) => {
        const user = bolsa.person_id?.user || {};
        const person = bolsa.person_id || {};
        return {
          nome: [user.first_name, user.last_name].filter(Boolean).join(' '),
          email: user.email || '',
          foto: user.avatar || null,
          cargo: person.person_type || 'Desconhecido',
          nivel: person.academic_level || 'Não definido'
        };
      })
    };
  });

  console.log('🚀 Equipas com líderes e membros:', equipasComLideres);
  return equipasComLideres;
}


export async function getEquipaDetalhes(equipaId, token) {
  const response = await fetch(`${API_URL}/items/teams/${equipaId}?fields=id,name,area,photo`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await response.json();
  if (!response.ok) throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar equipa');
  return data.data;
}

export async function createEquipa(equipa, token) {
  const response = await fetch(`${API_URL}/items/teams`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify({
      name: equipa.nome,
      area: equipa.descricao,
      status: 'ativo'
    })
  });

  const data = await response.json();
  if (!response.ok) throw new Error(data?.errors?.[0]?.message);
  return data.data;
}

// -----------------------------
// 👥 Membros
// -----------------------------
export async function createMembro(membro, token) {
  const response = await fetch(`${API_URL}/items/Membro`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify({
      Nome: membro.nome,
      Email: membro.email,
      Foto: membro.foto,
      Cargo: membro.role,
      equipa: membro.equipa
    })
  });

  const data = await response.json();
  if (!response.ok) throw new Error(data?.errors?.[0]?.message);
  return data.data;
}

export async function getMembrosPorEquipa(equipaId) {
  const response = await fetch(`${API_URL}/items/Bolsas?filter[team_id][_eq]=${equipaId}&fields=*,person_id.*,person_id.user.*`);

  const data = await response.json();
  if (!response.ok) throw new Error(data?.errors?.[0]?.message || 'Erro ao buscar membros da equipa');

  const bolsasPorPessoa = new Map();

  for (const bolsa of data.data) {
    const personId = bolsa.person_id?.id;
    if (!personId) continue;

    const anterior = bolsasPorPessoa.get(personId);
    const dataAtual = new Date(bolsa.start_date || 0);
    const dataAnterior = new Date(anterior?.start_date || 0);

    if (!anterior || dataAtual > dataAnterior) {
      bolsasPorPessoa.set(personId, bolsa);
    }
  }

  return Array.from(bolsasPorPessoa.values()).map(bolsa => {
    const user = bolsa.person_id?.user || {};
    const person = bolsa.person_id || {};

    return {
      id: user.id,
      nome: [user.first_name, user.last_name].filter(Boolean).join(' ') || 'Sem nome',
      email: user.email || 'Sem email',
      cargo: person.person_type || 'Desconhecido',
      nivel: person.academic_level || 'Não definido',
      status: (bolsa.status || '').toLowerCase() === 'activa' ? 'ativo' : 'inativo',
      foto: user.avatar || null,
      inicio: bolsa.start_date || null,
      fim: bolsa.end_date || null
    };
  });
}
export async function getPessoas(token) {
  const res = await fetch(
    `/api/items/persons?fields=id,user.first_name,user.last_name&limit=-1`,
    { headers: { Authorization: `Bearer ${token}` } }
  );

  const json = await res.json();
  if (!res.ok) throw new Error(json?.errors?.[0]?.message || 'Erro a buscar pessoas');

  return json.data.map(p => ({
    id  : p.id,                                             // ← este é o que vais gravar
    nome: [p.user?.first_name, p.user?.last_name]
            .filter(Boolean).join(' ') || '(sem nome)'
  }));

  
}


export async function updateUserInfo({ newName,newLastName, newEmail, newAvatar }, token) {
  const response = await fetch('/api/users/me', {
    method: 'PATCH',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify({
      first_name: newName,
      last_name : newLastName,
      email: newEmail,
      avatar: newAvatar,
    })
  });

  if (!response.ok) {
    throw new Error('Erro ao atualizar informações do utilizador');
  }

  return response.json();
}


// src/services/directus.js
export async function deletePublicacao(pubId, token) {
  const response = await fetch(`${API_URL}/items/publications/${pubId}`, {
    method : 'DELETE',
    headers: {
      Authorization : `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  // ✅ Directus devolve 204 No Content em sucesso
  if (response.status === 204) return true;

  // Se a resposta tiver JSON, lê (casos de erro normalmente trazem corpo)
  let data = null;
  const ctype = response.headers.get('content-type') || '';
  if (ctype.includes('application/json')) {
    data = await response.json();
  }

  if (!response.ok) {
    throw new Error(data?.errors?.[0]?.message || 'Erro ao deletar publicação');
  }

  return data; // pode ser null quando não há corpo
}

export async function editPublicacao(pub, token) {
const body = {
    title        : pub.title?.trim(),
    abstract     : pub.abstract?.trim(),
    status       : pub.status,
    doi_or_url   : (pub.doi_or_url || []).join('; '),
  };



  const response = await fetch(`${API_URL}/items/publications/${pub.id}`, {
    method: 'PATCH',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  });

  const resJson = await response.json();

  if (!response.ok) {
    console.error('❌ Erro ao editar publicação:', resJson?.errors?.[0]?.message);
    throw new Error(resJson?.errors?.[0]?.message || 'Erro ao editar publicação');
  }

  console.log('✅ Publicação editada com sucesso:', resJson);
  return resJson.data;
}
export async function uploadFile(file, token) {
  const form = new FormData();
  form.append('file', file);
  form.append('title', file.name);          // opcional, ajuda no admin
  const res = await fetch(`${API_URL}/files`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: form
  });
  const json = await res.json();
  if (!res.ok) throw new Error(json?.errors?.[0]?.message || 'Erro a carregar ficheiro');
  return json.data.id;                     // UUID do ficheiro
}

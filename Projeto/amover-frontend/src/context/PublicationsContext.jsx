import React, { createContext, useContext, useState, useEffect } from 'react';
import { useAuth } from '../services/Auth';
import { getPublicacoes } from '../services/directus';

const PublicationsContext = createContext();

export function PublicationsProvider({ children }) {
  const { token } = useAuth();
  const [publications, setPublications] = useState([]);
  const [loading, setLoading] = useState(true); // ➕ novo estado de loading

  useEffect(() => {
    if (!token) {
      setPublications([]);
      setLoading(false);
      return;
    }

    setLoading(true); // ⏳ inicia o loading
    getPublicacoes(token)
      .then(data => {
        const mapped = data.map(p => {
          const raw = (p.status || '').toLowerCase();
          let status;
          if (raw === 'publicado' || raw === 'published') status = 'publicar';
          else if (raw === 'rascunho' || raw === 'draft') status = 'rascunho';
          else if (raw === 'oculto' || raw === 'hidden') status = 'oculto';
          else status = raw;

          return {
            id: p.id,
            titulo: p.titulo,
            conteudo: p.conteudo,
            status,
            equipa: {
              id: p.equipa?.id || null,
              nome: p.equipa?.nome || 'Sem nome'
            },
            links: p.links || [],
            ficheiro: p.ficheiro,
            createdAt: p.data || null,
            autor:
              Array.isArray(p.Autores) && p.Autores.length > 0 && p.Autores[0].user
                ? {
                    id: p.Autores[0].user.id,
                    nome: `${p.Autores[0].user.first_name} ${p.Autores[0].user.last_name}`,
                    avatar: p.Autores[0].user.avatar || null
                  }
                : { id: null, nome: 'Desconhecido',avatar: null }
          };
        });

        setPublications(mapped);
      })
      .catch(err => {
        console.error('❌ Error loading publications:', err.message);
      })
      .finally(() => setLoading(false)); // ✅ termina o loading
  }, [token]);

  return (
    <PublicationsContext.Provider value={{ publications, setPublications, loading }}>
      {children}
    </PublicationsContext.Provider>
  );
}

export const usePublications = () => useContext(PublicationsContext);

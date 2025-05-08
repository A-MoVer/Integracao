import React, { createContext, useContext, useState } from 'react';

const PublicationsContext = createContext();

export function PublicationsProvider({ children }) {
  const [publications, setPublications] = useState([
    // Exemplo de mock inicial (opcional)
    // {
    //   id: 1,
    //   title: 'Teste A-MoVeR',
    //   content: 'Conteúdo de exemplo…',
    //   teamId: 1,
    //   tags: ['sustentabilidade'],
    //   author: 'ana@amover.pt',
    //   createdAt: new Date().toISOString(),
    // }
  ]);

  return (
    <PublicationsContext.Provider value={{ publications, setPublications }}>
      {children}
    </PublicationsContext.Provider>
  );
}

export function usePublications() {
  return useContext(PublicationsContext);
}

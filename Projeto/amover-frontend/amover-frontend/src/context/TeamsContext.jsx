// src/context/TeamsContext.jsx
import React, { createContext, useContext, useState } from 'react'

const TeamsContext = createContext()

export function TeamsProvider({ children }) {
  // MOCK INICIAL: coloque aqui 2 ou 3 equipas para teste
  const [equipas, setEquipas] = useState([
    {
      id: 1,
      nome: 'Inovação Verde',
      descricao: 'Desenvolvimento de soluções sustentáveis',
      membros: [
        { nome: 'Vanya', cargo: 'Bolseiro', email: 'vanya@example.com', foto: '' },
      ],
      status: 'ativo',
    },
    {
      id: 2,
      nome: 'Mobilidade Elétrica',
      descricao: 'Soluções de transporte ecológicas',
      membros: [
        { nome: 'Carlos', cargo: 'Técnico', email: 'carlos@example.com', foto: '' },
      ],
      status: 'ativo',
    },
  ])

  return (
    <TeamsContext.Provider value={{ equipas, setEquipas }}>
      {children}
    </TeamsContext.Provider>
  )
}

export function useTeams() {
  return useContext(TeamsContext)
}

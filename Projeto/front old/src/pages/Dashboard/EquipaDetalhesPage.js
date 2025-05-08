import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { getEquipaDetalhes } from '../../services/directus';
import HeaderLoggedIn from '../../components/afterLogin/HeaderLoggedIn';
import Footer from '../../components/Footer';
import './EquipaDetalhesPage.css';

function EquipaDetalhesPage() {
  const { id } = useParams();
  const [equipa, setEquipa] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showTooltip, setShowTooltip] = useState(false); // ✅ Definir o estado aqui
  const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:8055';

  useEffect(() => {
    async function fetchEquipa() {
      try {
        const token = localStorage.getItem('token');
        if (!token) {
          setError('Usuário não autenticado.');
          return;
        }
        const data = await getEquipaDetalhes(id, token);
        console.log("Dados da equipa recebidos:", data);
        setEquipa(data);
      } catch (err) {
        setError(err.message);
        console.error('Erro ao buscar equipa:', err);
      } finally {
        setLoading(false);
      }
    }
    fetchEquipa();
  }, [id]);

  return (
    <div className="equipa-detalhes-container">
      <HeaderLoggedIn showTooltip={showTooltip} setShowTooltip={setShowTooltip} />
      <main className="equipa-detalhes-content">
        {loading ? (
          <p>Carregando equipa...</p>
        ) : error ? (
          <p className="error-message">{error}</p>
        ) : (
          <>
            <h2>{equipa.Nome}</h2>
            {equipa.Descricao && (
    <p className="equipa-descricao">{equipa.Descricao}</p>
  )}
            <h3>Membros da Equipa</h3>
            <ul>
  {equipa.membros && equipa.membros.length > 0 ? (
    equipa.membros.map((membro) => (
      <li key={membro.id}>
        <div>
          <p>{membro.first_name} {membro.last_name}</p>

          {/* Exibir foto, caso exista */}
          {membro.Foto && membro.Foto.id ? (
            <img
              src={`${API_URL}/assets/${membro.Foto.id}`}
              alt={`Foto de ${membro.first_name}`}
              style={{ width: '100px', height: 'auto' }}
            />
          ) : (
            <p>Sem foto</p>
          )}
        </div>
      </li>
    ))
  ) : (
    <p>Sem membros atribuídos</p>
  )}
</ul>

          </>
        )}
      </main>
      <Footer />
    </div>
  );
}

export default EquipaDetalhesPage;

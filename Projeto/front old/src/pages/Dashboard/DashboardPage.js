import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import HeaderLoggedIn from '../../components/afterLogin/HeaderLoggedIn';
import Footer from '../../components/Footer';
import { getPublicacoes } from '../../services/directus'; 
import { FaPlus } from 'react-icons/fa'; 
import './DashboardPage.css';

function DashboardPage() {
  const [publicacoes, setPublicacoes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showTooltip, setShowTooltip] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    async function fetchData() {
      try {
        const token = localStorage.getItem('token');
        if (!token) {
          setError('Usuário não autenticado.');
          return;
        }
        const data = await getPublicacoes(token);
        setPublicacoes(data);
      } catch (err) {
        setError(err.message);
        console.error('Erro ao buscar publicações:', err);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const handleEquipasClick = () => {
    navigate('/dashboard/equipas');
  };

  const handleNewPostClick = () => {
    navigate('/dashboard/NewPost');
  };

  return (
    <div className="dashboard-container">
      <HeaderLoggedIn showTooltip={showTooltip} setShowTooltip={setShowTooltip} />

      <main className="dashboard-content">
        <div className="header-content">
          <h2>Últimas Publicações</h2>

          {/* Botão para criar nova publicação */}
          <button className="new-post-button" onClick={handleNewPostClick}>
            <FaPlus className="plus-icon" />
          </button>

          {/* Botão para ver equipas */}
          <button className="equipas-button" onClick={handleEquipasClick}>
            Ver Equipas
          </button>
        </div>

        {loading ? (
          <p>Carregando publicações...</p>
        ) : error ? (
          <p className="error-message">{error}</p>
        ) : (
          <div className="publicacoes-list">
            {publicacoes.map((pub) => (
              <div key={pub.id} className="publicacao-card">
                <h3>{pub.titulo}</h3>
                <p>
                  {pub.conteudo ? pub.conteudo.substring(0, 150) + '...' : ''}
                </p>
                <small>
                  Publicado em: {new Date(pub.date_created).toLocaleDateString()}
                </small>
              </div>
            ))}
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}

export default DashboardPage;

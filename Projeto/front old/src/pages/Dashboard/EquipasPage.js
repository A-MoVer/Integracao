// EquipasPage.js
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getEquipasComMembros } from '../../services/directus';
import HeaderLoggedIn from '../../components/afterLogin/HeaderLoggedIn';
import Footer from '../../components/Footer';
import getMaterialIcon from '../../services/getMaterialIcon';
import './EquipasPage.css';

function EquipasPage() {
  const navigate = useNavigate(); 
  const [equipas, setEquipas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showTooltip, setShowTooltip] = useState(false);

  useEffect(() => {
    async function fetchData() {
      try {
        const token = localStorage.getItem('token');
        if (!token) {
          setError('Usuário não autenticado.');
          return;
        }
        const data = await getEquipasComMembros(token);
        setEquipas(data);
      } catch (err) {
        setError(err.message);
        console.error('Erro ao buscar equipas:', err);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  return (
    <div className="equipas-container">
      <HeaderLoggedIn showTooltip={showTooltip} setShowTooltip={setShowTooltip} />
      <main className="equipas-content">
        <h2>Equipas</h2>
        {loading ? (
          <p>Carregando equipas...</p>
        ) : error ? (
          <p className="error-message">{error}</p>
        ) : (
          <div className="equipas-list">
            {equipas.map((equipa) => {
            const IconComponent = getMaterialIcon(equipa.Logo); 

            return (
                <div key={equipa.id} className="equipa-card" onClick={() => navigate(`/dashboard/equipas/${equipa.id}`)}>
                <IconComponent fontSize="large" />
                <h3>{equipa.Nome}</h3>
                </div>
            );
            })}
          </div>
        )}
      </main>
      <Footer />
    </div>
  );
}

export default EquipasPage;

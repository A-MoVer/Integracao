import React, { useState } from 'react';
import './TeamsPage.css';
import Header from '../components/Header';
import Footer from '../components/Footer';


function TeamsPage() {
  // Exemplo de estado para controlar o tooltip
  const [showTooltip, setShowTooltip] = useState(false);

  return (
    <div className="teams-container">
      <video autoPlay muted loop className="background-video">
        <source src="/video/a-mover.mp4" type="video/mp4" />
      </video>
      <div className="video-overlay" />

      <Header showTooltip={showTooltip} setShowTooltip={setShowTooltip} />

      <main className="main-content">
        <div className="content-card">
          <section className="intro">
            <h2>Equipas do Projeto</h2>
            <p>
              Conheça as equipas responsáveis por impulsionar o desenvolvimento 
              e a inovação na Agenda A‑MoVeR.
            </p>
            <div className="teams-list">
              <div className="team-card">
                <h3>Equipe de Desenvolvimento</h3>
                <p>Responsável por criar as soluções tecnológicas inovadoras.</p>
              </div>
              <div className="team-card">
                <h3>Equipe de Marketing</h3>
                <p>Cuida da divulgação e comunicação da Agenda A‑MoVeR.</p>
              </div>
              <div className="team-card">
                <h3>Equipe de Operações</h3>
                <p>Gerencia as operações diárias e a logística do projeto.</p>
              </div>
              <div className="team-card">
                <h3>Equipe de Suporte Técnico</h3>
                <p>
                  Oferece suporte e manutenção para garantir a performance 
                  das soluções.
                </p>
              </div>
            </div>
          </section>
        </div>
      </main>

      <Footer />
    </div>
  );
}

export default TeamsPage;

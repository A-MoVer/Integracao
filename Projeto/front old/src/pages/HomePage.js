// HomePage.js
import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './HomePage.css';

import Header from '../components/Header';
import Footer from '../components/Footer';

function HomePage() {
  const navigate = useNavigate();
  const [animate, setAnimate] = useState(false);
  const containerRef = useRef(null);
  const targetRef = useRef(null);
  const [mousePos, setMousePos] = useState({ x: 0, y: 0 });
  const [showTooltip, setShowTooltip] = useState(true);

  const handleMouseMove = (e) => {
    setMousePos({ x: e.clientX, y: e.clientY });
  };

  const handleMainContentClick = () => {
    if (containerRef.current && targetRef.current) {
      const containerRect = containerRef.current.getBoundingClientRect();
      const targetRect = targetRef.current.getBoundingClientRect();
      const originX =
        ((targetRect.left + targetRect.width / 2) - containerRect.left) /
        containerRect.width *
        100;
      const originY =
        ((targetRect.top + targetRect.height / 2) - containerRect.top) /
        containerRect.height *
        100;
      containerRef.current.style.transformOrigin = `${originX}% ${originY}%`;
    }
    setAnimate(true);
    setTimeout(() => {
      navigate('/equipas');
    }, 2000);
  };

  return (
    <div
      ref={containerRef}
      className={`home-container ${animate ? 'camera-zoom' : ''}`}
      onMouseMove={handleMouseMove}
    >
      {showTooltip && (
        <div
          style={{
            position: 'fixed',
            left: mousePos.x + 10,
            top: mousePos.y + 10,
            pointerEvents: 'none',
            color: '#fff',
            fontSize: '14px',
            backgroundColor: 'rgba(0,0,0,0.5)',
            padding: '5px 10px',
            borderRadius: '5px',
          }}
        >
          Para saber mais, clique aqui
        </div>
      )}

      <video autoPlay muted loop className="background-video">
        <source src="/video/a-mover.mp4" type="video/mp4" />
      </video>
      <div className="video-overlay" />

      <Header showTooltip={showTooltip} setShowTooltip={setShowTooltip} />

      <main className="main-content" onClick={handleMainContentClick}>
        <div className="content-card">
          <section className="intro">
            <h2>
              Bem-vindo à Agenda A-M
              <span ref={targetRef}>o</span>
              VeR
            </h2>
            <p>
              A Agenda A-MoVeR pretende realizar uma alteração substancial no
              modelo de desenvolvimento industrial na Região de Trás-os-Montes e
              Alto Douro, através de um conjunto de investimentos industriais que
              permitirão a inserção das empresas participantes em segmentos
              internacionais de elevado valor acrescentado.
            </p>
            <p>
              Serão criados 3 PPS (Produtos, Processos e Serviços) inovadores a
              nível mundial:
            </p>
            <ul>
              <li>
                <strong>Antena Inteligente Vehicle-to-Everything:</strong> Comunicação
                entre veículos e outros atores da mobilidade via soluções modulares
                para 5G.
              </li>
              <li>
                <strong>Motociclo elétrico de elevada autonomia:</strong> Voltado para
                promover uma mobilidade urbana cómoda, eficiente e verde.
              </li>
              <li>
                <strong>Serviços de recolha/gestão seguras de informação em tempo
                real:</strong> Incremento de valor para o cluster automóvel da Região,
                integrando Smart Manufacturing e processos de fabrico avançados numa
                lógica de “fábrica do futuro”.
              </li>
            </ul>
          </section>
        </div>
      </main>

      <Footer showTooltip={showTooltip} setShowTooltip={setShowTooltip} />
    </div>
  );
}

export default HomePage;

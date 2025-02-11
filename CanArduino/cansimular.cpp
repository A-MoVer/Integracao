#include <iostream>
#include <string>
#include <fcntl.h>
#include <termios.h>
#include <unistd.h>
#include <cstring>
#include <errno.h>
#include <cstdlib>
#include <thread>
#include <atomic>
#include <sstream>   

// Sequência de cores para saída no terminal
#define COLOR_RESET   "\033[0m"
#define COLOR_RED     "\033[31m"
#define COLOR_GREEN   "\033[32m"
#define COLOR_YELLOW  "\033[33m"
#define COLOR_BLUE    "\033[34m"
#define COLOR_MAGENTA "\033[35m"
#define COLOR_CYAN    "\033[36m"
#define COLOR_WHITE   "\033[37m"

// Variáveis globais para facilitar o exemplo
static std::atomic<bool> g_running(true);   // controla se o programa continua rodando
static int g_fdSerial = -1;                 // descritor da porta serial

static int currentSpeed = 0;                // velocidade simulada
static int lastValue = -1;                  // estado do pisca (0=idle,1=esq,2=dir)

// ------------------------------------------------------------------
// Funções de exibição (dashboard)
// ------------------------------------------------------------------

void showBanner() {
    std::cout << COLOR_CYAN << R"(
  
  _____            _____            __        __        
 / ___/__ ____    / __(_)_ _  __ __/ /__ ____/ /__  ____
/ /__/ _ `/ _ \  _\ \/ /  ' \/ // / / _ `/ _  / _ \/ __/
/___/\_,_/_//_/ /___/_/_/_/_/\_,_/_/\_,_/\_,_/\___/_/   

   Simular Leitura/Escrita CAN via Serial (Dashboard)
)" << COLOR_RESET << std::endl;
}

void drawSpeedometer(int speed) {
    const int maxSpeed = 100; 
    if (speed > maxSpeed) {
        speed = maxSpeed; 
    }

    const int barWidth = 20;
    int fill = (speed * barWidth) / maxSpeed;

    // Monta a barra
    std::string bar = "[";
    for (int i = 0; i < barWidth; i++) {
        bar += (i < fill) ? "#" : " ";
    }
    bar += "]";

    if (speed < 40) {
        std::cout << COLOR_GREEN;
    } else if (speed < 70) {
        std::cout << COLOR_YELLOW;
    } else {
        std::cout << COLOR_RED;
    }
    std::cout << "Velocidade: " << bar << " " << speed << " km/h\n" << COLOR_RESET;
}

void displayDashboard(int stateValue, const std::string &rawLine) {
    // Limpa a tela
    std::cout << "\033[2J\033[H";

    std::cout << COLOR_CYAN 
              << "         CAN SIMULADOR - DASHBOARD\n" 
              << "------------------------------------------------------\n\n"
              << COLOR_RESET;

    switch (stateValue) {
        case 0:
            std::cout << COLOR_WHITE << "[IDLE] Nenhum pisca ligado.\n" << COLOR_RESET;
            break;

        case 1:
            std::cout << COLOR_GREEN
                      << "   <-- Pisca para Esquerda\n\n"
                      << COLOR_RESET;
            std::cout << COLOR_GREEN << R"(  
              .--------.
  ← ← ← ← ←    | ESQ    |
              '--------'
)" << COLOR_RESET << std::endl;
            break;

        case 2:
            std::cout << COLOR_GREEN
                      << "   --> Pisca para Direita\n\n"
                      << COLOR_RESET;
            std::cout << COLOR_GREEN << R"(
              .--------.
              | DIR    |   → → → → →
              '--------'
)" << COLOR_RESET << std::endl;
            break;

        default:
            std::cout << COLOR_RED 
                      << "[ERRO] Estado desconhecido: " 
                      << stateValue << "\n" 
                      << COLOR_RESET;
            break;
    }

    drawSpeedometer(currentSpeed);

    std::cout << COLOR_BLUE << "\n[RAW] " << COLOR_RESET << rawLine << std::endl;
    std::cout << "\n------------------------------------------------------\n";
}

// ------------------------------------------------------------------
// Funções de envio via serial
// ------------------------------------------------------------------

void sendCanData(int fd, int value) {
    // Monta string no formato "DATA=0x##\n"
    std::stringstream ss;
    ss << "DATA=0x" << std::hex << value << "\n";
    std::string out = ss.str();

    // Escreve na porta serial
    int ret = write(fd, out.c_str(), out.size());
    if (ret < 0) {
        std::cerr << COLOR_RED << "[ERRO] Falha ao enviar dados: "
                  << strerror(errno) << COLOR_RESET << std::endl;
    } else {
        std::cout << COLOR_YELLOW << "[ENVIO] " 
                  << out << COLOR_RESET;
    }
}

// (Opcional) Se quiser enviar também velocidade:
void sendCanSpeed(int fd, int speedValue) {
    std::stringstream ss;
    ss << "SPEED=0x" << std::hex << speedValue << "\n";
    std::string out = ss.str();

    int ret = write(fd, out.c_str(), out.size());
    if (ret < 0) {
        std::cerr << COLOR_RED << "[ERRO] Falha ao enviar velocidade: "
                  << strerror(errno) << COLOR_RESET << std::endl;
    } else {
        std::cout << COLOR_YELLOW << "[ENVIO] " 
                  << out << COLOR_RESET;
    }
}

// ------------------------------------------------------------------
// Thread que lê o teclado (stdin) e envia comandos CAN
// ------------------------------------------------------------------

void inputThreadFunc(int fd) {
    // Instruções simples ao usuário
    std::cout << COLOR_WHITE
              << "[INPUT] Você pode digitar comandos abaixo:\n"
              << "  '0'  -> DATA=0x00 (pisca OFF)\n"
              << "  '1'  -> DATA=0x01 (pisca ESQ)\n"
              << "  '2'  -> DATA=0x02 (pisca DIR)\n"
              << "  'vXX'-> SPEED=0xXX (velocidade)\n"
              << "  'exit' ou Ctrl+D -> encerrar\n\n"
              << COLOR_RESET;

    while (g_running) {
        std::string line;
        if (!std::getline(std::cin, line)) {
            // Se o usuário der Ctrl+D, getline falha -> encerrar
            std::cerr << COLOR_RED << "[INPUT] Entrada padrão fechada, saindo...\n" 
                      << COLOR_RESET;
            g_running = false;
            break;
        }

        if (line == "exit") {
            // Comando para sair
            g_running = false;
            break;
        }

        // Trata alguns comandos simples
        if (line == "0") {
            sendCanData(fd, 0x00);
        } else if (line == "1") {
            sendCanData(fd, 0x01);
        } else if (line == "2") {
            sendCanData(fd, 0x02);
        } 
        else if (line.rfind("v", 0) == 0) {
            // Se começa com 'v', tentamos extrair valor
            // Exemplo: "v3C" -> SPEED=0x3C
            std::string hexSpeed = line.substr(1); // remove o 'v'
            int spd = 0;
            try {
                spd = std::stoi(hexSpeed, nullptr, 16);
                sendCanSpeed(fd, spd);
            } catch (...) {
                std::cerr << COLOR_RED 
                          << "[ERRO] Valor de velocidade inválido: " << line 
                          << COLOR_RESET << std::endl;
            }
        }
        else {
            std::cerr << COLOR_RED << "[ERRO] Comando inválido: " << line 
                      << COLOR_RESET << std::endl;
        }
    }
}

// ------------------------------------------------------------------
// Função principal (thread principal lê da serial e desenha dashboard)
// ------------------------------------------------------------------

int main() {
    showBanner();

    std::cout << COLOR_YELLOW
              << "[INFO] Abrindo porta serial /dev/ttyACM0 em 115200 baud...\n"
              << COLOR_RESET;

    g_fdSerial = open("/dev/ttyACM0", O_RDWR | O_NOCTTY);
    if (g_fdSerial < 0) {
        std::cerr << COLOR_RED
                  << "[ERRO] Falha ao abrir /dev/ttyACM0: "
                  << strerror(errno) << COLOR_RESET << std::endl;
        return 1;
    }

    // Configura a porta serial
    struct termios tty;
    memset(&tty, 0, sizeof(tty));

    if (tcgetattr(g_fdSerial, &tty) != 0) {
        std::cerr << COLOR_RED
                  << "[ERRO] tcgetattr: " << strerror(errno)
                  << COLOR_RESET << std::endl;
        close(g_fdSerial);
        return 1;
    }

    cfsetispeed(&tty, B115200);
    cfsetospeed(&tty, B115200);

    tty.c_cflag &= ~CSIZE;
    tty.c_cflag |= CS8;
    tty.c_cflag &= ~PARENB;
    tty.c_cflag &= ~CSTOPB;
    tty.c_cflag |= (CLOCAL | CREAD);

    tty.c_lflag &= ~(ICANON | ECHO | ECHOE | ISIG);
    tty.c_oflag &= ~OPOST;

    if (tcsetattr(g_fdSerial, TCSANOW, &tty) != 0) {
        std::cerr << COLOR_RED
                  << "[ERRO] tcsetattr: " << strerror(errno)
                  << COLOR_RESET << std::endl;
        close(g_fdSerial);
        return 1;
    }

    std::cout << COLOR_GREEN
              << "[OK] Porta /dev/ttyACM0 aberta e configurada com sucesso!\n"
              << COLOR_RESET << std::endl;

    std::cout << COLOR_WHITE
              << "Aguardando dados do Arduino...\n"
              << "Pressione Ctrl+C ou digite 'exit' para encerrar.\n"
              << COLOR_RESET << std::endl;
    sleep(1);

    displayDashboard(0, "Inicializando...");

    std::thread inputThread(inputThreadFunc, g_fdSerial);

    std::string buffer;
    while (g_running) {
        char buf[256];
        memset(buf, 0, sizeof(buf));
        int n = read(g_fdSerial, buf, sizeof(buf) - 1);

        if (n > 0) {
            buffer.append(buf, n);

            std::size_t pos;
            while ((pos = buffer.find('\n')) != std::string::npos) {
                std::string line = buffer.substr(0, pos);
                buffer.erase(0, pos + 1);

                bool updated = false; 

                // 1) Verifica DATA=0x
                {
                    std::size_t dataPos = line.find("DATA=0x");
                    if (dataPos != std::string::npos) {
                        // "DATA=0x" tem 7 chars
                        std::string hexValue = line.substr(dataPos + 7);
                        int value = 0;
                        try {
                            value = std::stoi(hexValue, nullptr, 16);
                        } catch (...) {
                            std::cerr << COLOR_RED
                                      << "[ERRO] Falha ao converter valor hexadecimal na linha: "
                                      << line << COLOR_RESET << std::endl;
                            continue;
                        }
                        if (value != lastValue) {
                            lastValue = value;
                            updated = true;
                        }
                    }
                }

                // 2) Verifica SPEED=0x
                {
                    std::size_t speedPos = line.find("SPEED=0x");
                    if (speedPos != std::string::npos) {
                        // "SPEED=0x" tem 8 chars
                        std::string hexValue = line.substr(speedPos + 8);
                        int speed = 0;
                        try {
                            speed = std::stoi(hexValue, nullptr, 16);
                        } catch (...) {
                            std::cerr << COLOR_RED
                                      << "[ERRO] Falha ao converter valor de velocidade na linha: "
                                      << line << COLOR_RESET << std::endl;
                            continue;
                        }
                        if (speed != currentSpeed) {
                            currentSpeed = speed;
                            updated = true;
                        }
                    }
                }

                if (updated) {
                    displayDashboard(lastValue, line);
                }
            }
        }
        else if (n < 0) {
            std::cerr << COLOR_RED
                      << "[ERRO] Falha ao ler /dev/ttyACM0: "
                      << strerror(errno) << COLOR_RESET << std::endl;
            g_running = false;
        }
        else {
            usleep(1000 * 100);
        }
    }

    std::cout << COLOR_RED << "\nEncerrando o programa...\n" << COLOR_RESET;
    close(g_fdSerial);

    if (inputThread.joinable()) {
        inputThread.join();
    }

    return 0;
}

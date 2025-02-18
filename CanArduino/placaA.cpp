#include <iostream>
#include <cstring>
#include <cstdlib>
#include <fcntl.h>
#include <termios.h>
#include <unistd.h>
#include <cerrno>
#include <sys/types.h>
#include <sys/socket.h>
#include <linux/can.h>
#include <linux/can/raw.h>
#include <net/if.h>
#include <sys/ioctl.h>
#include <atomic>
#include <thread>

// Variáveis globais para controle
static std::atomic<bool> g_running(true);
static int g_fdSerial = -1;
static int g_fdCan = -1;  // Socket CAN

// ─── Função para abrir e configurar a porta serial ─────────────────────────
int openSerial(const char* port) {
    int fd = open(port, O_RDWR | O_NOCTTY | O_SYNC);
    if (fd < 0) {
        std::cerr << "[ERRO] Falha ao abrir a porta serial " << port << "!" << std::endl;
        std::exit(EXIT_FAILURE);
    }

    struct termios tty;
    std::memset(&tty, 0, sizeof tty);
    if (tcgetattr(fd, &tty) != 0) {
        std::cerr << "[ERRO] tcgetattr falhou: " << strerror(errno) << std::endl;
        std::exit(EXIT_FAILURE);
    }

    // Configura a taxa de transmissão para 115200 bps (ajuste se necessário)
    cfsetospeed(&tty, B115200);
    cfsetispeed(&tty, B115200);

    // Configura 8N1 (8 bits, sem paridade, 1 stop bit)
    tty.c_cflag = (tty.c_cflag & ~CSIZE) | CS8;
    tty.c_iflag &= ~IGNBRK;         // desabilita o tratamento de break
    tty.c_lflag = 0;                // modo não canônico, sem echo, etc.
    tty.c_oflag = 0;                // sem remapeamento
    tty.c_cc[VMIN]  = 0;            // leitura não bloqueante
    tty.c_cc[VTIME] = 5;            // timeout de 0.5 s

    tty.c_iflag &= ~(IXON | IXOFF | IXANY); // desabilita controle de fluxo
    tty.c_cflag |= (CLOCAL | CREAD);  // ignora controles de modem, habilita leitura
    tty.c_cflag &= ~(PARENB | PARODD);      // sem paridade
    tty.c_cflag &= ~CSTOPB;
    tty.c_cflag &= ~CRTSCTS;         // sem controle de fluxo por hardware

    if (tcsetattr(fd, TCSANOW, &tty) != 0) {
        std::cerr << "[ERRO] tcsetattr falhou: " << strerror(errno) << std::endl;
        std::exit(EXIT_FAILURE);
    }
    return fd;
}

// ─── Função para abrir e configurar a interface CAN ─────────────────────────
int openCAN(const char* ifname) {
    int can_socket = socket(PF_CAN, SOCK_RAW, CAN_RAW);
    if (can_socket < 0) {
        std::cerr << "[ERRO] Falha ao abrir o socket CAN!" << std::endl;
        std::exit(EXIT_FAILURE);
    }

    struct ifreq ifr;
    std::snprintf(ifr.ifr_name, sizeof(ifr.ifr_name), "%s", ifname);
    ifr.ifr_name[sizeof(ifr.ifr_name) - 1] = '\0';
    if (ioctl(can_socket, SIOCGIFINDEX, &ifr) < 0) {
        std::cerr << "[ERRO] ioctl para interface CAN falhou!" << std::endl;
        std::exit(EXIT_FAILURE);
    }

    struct sockaddr_can addr;
    std::memset(&addr, 0, sizeof(addr));
    addr.can_family = AF_CAN;
    addr.can_ifindex = ifr.ifr_ifindex;

    if (bind(can_socket, reinterpret_cast<struct sockaddr*>(&addr), sizeof(addr)) < 0) {
        std::cerr << "[ERRO] Falha ao dar bind na interface CAN!" << std::endl;
        std::exit(EXIT_FAILURE);
    }

    return can_socket;
}

// ─── Função para enviar uma mensagem CAN ─────────────────────────
void sendCanMessage(int can_fd, int id, const std::string& data) {
    struct can_frame frame;
    // Zera o frame para evitar lixo nos bytes não utilizados
    std::memset(&frame, 0, sizeof(frame));

    frame.can_id = id;
    // Limita o tamanho dos dados a 8 bytes, se necessário
    frame.can_dlc = (data.size() > 8) ? 8 : data.size();
    std::memcpy(frame.data, data.c_str(), frame.can_dlc);

    if (write(can_fd, &frame, sizeof(frame)) != sizeof(frame)) {
        std::cerr << "[ERRO] Falha ao enviar mensagem CAN!" << std::endl;
    } else {
        std::cout << "[CAN] Enviado: ID=0x" << std::hex << id 
                  << " Data=" << data << std::dec << std::endl;
    }
}

// ─── Função para processar uma linha recebida da serial e enviar via CAN ─────────
void processReceivedData(const std::string &line) {
    // Procura os marcadores na string (conforme o formato enviado pelo Arduino)
    std::size_t dataPos  = line.find("DATA=0x");
    std::size_t speedPos = line.find("SPEED=0x");
    std::size_t accelPos = line.find("ACCEL=0x");
    std::size_t brakePos = line.find("BRAKE=0x");
    std::size_t distPos  = line.find("DIST=0x");

    if (dataPos != std::string::npos) {
        std::string hexData = line.substr(dataPos + 7);
        sendCanMessage(g_fdCan, 0x100, hexData);
    }
    if (speedPos != std::string::npos) {
        std::string hexData = line.substr(speedPos + 8);
        sendCanMessage(g_fdCan, 0x200, hexData);
    }
    if (accelPos != std::string::npos) {
        std::string hexData = line.substr(accelPos + 8);
        sendCanMessage(g_fdCan, 0x300, hexData);
    }
    if (brakePos != std::string::npos) {
        std::string hexData = line.substr(brakePos + 8);
        sendCanMessage(g_fdCan, 0x400, hexData);
    }
    if (distPos != std::string::npos) {
        std::string hexData = line.substr(distPos + 7);
        sendCanMessage(g_fdCan, 0x500, hexData);
    }
}

// ─── Função principal ────────────────────────────────────────────────
int main() {
    const char* serialPort  = "/dev/ttyACM0";  // Ajuste conforme necessário
    const char* canInterface = "can0";

    // Abre a interface CAN
    g_fdCan = openCAN(canInterface);

    // Abre a porta serial
    g_fdSerial = openSerial(serialPort);

    std::cout << "[INFO] Iniciando a leitura da porta serial e envio ao CAN..." << std::endl;

    // Buffer para armazenar dados lidos da serial
    std::string buffer;
    const size_t BUF_SIZE = 256;
    char buf[BUF_SIZE];

    // Loop principal de leitura da porta serial
    while (g_running) {
        std::memset(buf, 0, sizeof(buf));
        int n = read(g_fdSerial, buf, std::min(BUF_SIZE, static_cast<size_t>(MAX_READ_SIZE))); 
        if (n > 0) {
            buffer.append(buf, n);
            // Processa cada linha completa (delimitada por '\n')
            std::size_t pos;
            while ((pos = buffer.find('\n')) != std::string::npos) {
                std::string line = buffer.substr(0, pos);
                buffer.erase(0, pos + 1);
                processReceivedData(line);
            }
        } else if (n < 0) {
            std::cerr << "[ERRO] Falha ao ler da serial: " << strerror(errno) << std::endl;
            g_running = false;
        } else {
            // Se não há dados, aguarda um pouco para não consumir 100% da CPU
            nanosleep(&(struct timespec){0, 100000000}, NULL);        }
    }

    close(g_fdSerial);
    close(g_fdCan);
    return 0;
}

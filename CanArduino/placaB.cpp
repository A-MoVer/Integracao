#include <iostream>
#include <sstream>
#include <string>
#include <thread>
#include <atomic>
#include <chrono>
#include <cstring>
#include <cstdlib>
#include <fcntl.h>
#include <unistd.h>
#include <ncurses.h>
#include <linux/can.h>
#include <linux/can/raw.h>
#include <net/if.h>
#include <sys/ioctl.h>
#include <algorithm>
#include <cctype>
#include <locale>

// Função trim para remover espaços
static inline std::string trim(const std::string &s) {
    auto start = s.begin();
    while (start != s.end() && std::isspace(*start)) {
        start++;
    }
    auto end = s.end();
    do {
        end--;
    } while (std::distance(start, end) > 0 && std::isspace(*end));
    return std::string(start, end + 1);
}

// ─── Variáveis globais ───────────────────────────────
static std::atomic<bool> g_running(true);
static int g_canSocket = -1;

// Variáveis dos dados CAN recebidos
static int blinkState    = 0; // 0 = OFF, 1 = Pisca Esquerda, 2 = Pisca Direita
static int currentSpeed  = 0; // Velocidade
static int accelPercent  = 0; // Acelerador (em %)
static int brakePercent  = 0; // Travão (em %)
static int distanceValue = 0; // Distância (em cm)
static std::string lastCanMsg; // Última mensagem CAN recebida (para exibição)

// ─── Funções de exibição (Dashboard) ───────────────────────────────

void showBanner(WINDOW *win) {
    werase(win);
    wattron(win, COLOR_PAIR(1)); // cor CIANO
    wprintw(win, "\n  _____            _____            __        __        \n");
    wprintw(win, " / ___/__ ____    / __(_)_ _  __ __/ /__ ____/ /__  ____\n");
    wprintw(win, "/ /__/ _ `/ _ \\  _\\ \\/ /  ' \\/ // / / _ `/ _  / _ \\/ __/\n");
    wprintw(win, "/___/\\_,_/_//_/ /___/_/_/_/_/\\_,_/_/\\_,_/\\_,_/\\___/_/   \n\n");
    wprintw(win, "   Recepção CAN via Socket (Dashboard)\n\n");
    wattroff(win, COLOR_PAIR(1));
    wrefresh(win);
}

void drawBlinkers(WINDOW *win, int state) {
    if (state == 1) { // Pisca Esquerda
        wattron(win, COLOR_PAIR(2)); // verde
        wprintw(win, "Pisca ESQUERDA ativado\n");
        wprintw(win, "    *\n"
                      "   **\n"
                      "  ***\n"
                      " ****\n"
                      "  ***\n"
                      "  ***\n"
                      "   **\n"
                      "    *\n");
        wattroff(win, COLOR_PAIR(2));
    }
    else if (state == 2) { // Pisca Direita
        wattron(win, COLOR_PAIR(2));
        wprintw(win, "Pisca DIREITA ativado\n");
        wprintw(win, "*    \n"
                      "**   \n"
                      "***  \n"
                      "**** \n"
                      "***  \n"
                      "***  \n"
                      "**   \n"
                      "*    \n");
        wattroff(win, COLOR_PAIR(2));
    }
    else {
        wattron(win, COLOR_PAIR(7)); // branco
        wprintw(win, "Nenhum pisca ativado.\n");
        wattroff(win, COLOR_PAIR(7));
    }
}

void drawSpeedometer(WINDOW *win, int speed) {
    const int minSpeed = 0;
    const int maxSpeed = 120;
    const int gaugeWidth = 40;  // largura do medidor

    int quarter = gaugeWidth / 4;
    int half = gaugeWidth / 2;
    int threeQuarter = (3 * gaugeWidth) / 4;

    std::string gaugeBar(gaugeWidth, '-');
    gaugeBar[0] = '|';
    gaugeBar[quarter] = '|';
    gaugeBar[half] = '|';
    gaugeBar[threeQuarter] = '|';
    gaugeBar[gaugeWidth - 1] = '|';

    std::stringstream scaleLine;
    scaleLine << minSpeed << " " << gaugeBar << " " << maxSpeed << " km/h";

    int pointerPos = (speed * (gaugeWidth - 1)) / maxSpeed;
    std::string pointerLine(gaugeWidth, ' ');
    if (pointerPos >= 0 && pointerPos < gaugeWidth)
        pointerLine[pointerPos] = '^';

    std::stringstream speedLine;
    speedLine << "Velocidade: " << speed << " km/h";

    int color = 2; // verde
    if (speed >= 40 && speed < 70)
        color = 3; // amarelo
    else if (speed >= 70)
        color = 4; // vermelho

    wattron(win, COLOR_PAIR(color));
    wprintw(win, "%s\n", speedLine.str().c_str());
    wprintw(win, "%s\n", scaleLine.str().c_str());
    wprintw(win, "  %s\n", pointerLine.c_str());
    wattroff(win, COLOR_PAIR(color));
}

void displayDashboard(WINDOW *win, const std::string &rawMsg) {
    werase(win);
    wattron(win, COLOR_PAIR(1));
    wprintw(win, "         CAN SIMULADOR - DASHBOARD\n");
    wprintw(win, "------------------------------------------------------\n\n");
    wattroff(win, COLOR_PAIR(1));

    drawBlinkers(win, blinkState);
    drawSpeedometer(win, currentSpeed);

    wattron(win, COLOR_PAIR(5)); // magenta
    wprintw(win, "\nAcelerador: %d%%  Travão: %d%%  Distância: %d cm\n",
            accelPercent, brakePercent, distanceValue);
    wattroff(win, COLOR_PAIR(5));

    wattron(win, COLOR_PAIR(6)); // azul
    wprintw(win, "\n[CAN] %s\n", rawMsg.c_str());
    wattroff(win, COLOR_PAIR(6));

    wprintw(win, "\n------------------------------------------------------\n");
    wprintw(win, "Pressione 'q' para sair.\n");
    wrefresh(win);
}

int openCAN(const char* ifname) {
    int can_socket = socket(PF_CAN, SOCK_RAW, CAN_RAW);
    if (can_socket < 0) {
        std::cerr << "[ERRO] Falha ao abrir o socket CAN!" << std::endl;
        std::exit(EXIT_FAILURE);
    }

    struct ifreq ifr;
    std::strcpy(ifr.ifr_name, ifname);
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

// ─── Thread de leitura CAN ───────────────────────────────
void canReceiveThreadFunc() {
    struct can_frame frame;
    while (g_running) {
        int n = read(g_canSocket, &frame, sizeof(frame));
        if (n > 0) {
            std::stringstream ss;
            ss << "CAN recebido: ID=0x" << std::hex << frame.can_id << " Dados: ";
            for (int i = 0; i < frame.can_dlc; i++) {
                ss << std::hex << static_cast<int>(frame.data[i]) << " ";
            }
            lastCanMsg = ss.str();

            // Extrai a string dos dados e remove espaços extras
            std::string raw(reinterpret_cast<char*>(frame.data), frame.can_dlc);
            std::string str = trim(raw);

            switch(frame.can_id) {
                case 0x100: // DATA (estado dos piscas) – valor binário
                    blinkState = frame.data[0];
                    break;
                case 0x200: // SPEED – interpretado como hexadecimal
                {
                    try {
                        currentSpeed = std::stoi(str, nullptr, 16);
                    } catch(...) {
                        currentSpeed = 0;
                    }
                }
                break;
                case 0x300: // ACCEL – interpretado como hexadecimal
                {
                    try {
                        accelPercent = std::stoi(str, nullptr, 16);
                    } catch(...) {
                        accelPercent = 0;
                    }
                }
                break;
                case 0x400: // BRAKE – interpretado como hexadecimal
                {
                    try {
                        brakePercent = std::stoi(str, nullptr, 16);
                    } catch(...) {
                        brakePercent = 0;
                    }
                }
                break;
                case 0x500: // DIST – interpretado como hexadecimal
                {
                    try {
                        distanceValue = std::stoi(str, nullptr, 16);
                    } catch(...) {
                        distanceValue = 0;
                    }
                }
                break;
                default:
                    break;
            }
            
        }
        else if (n < 0) {
            std::cerr << "[ERRO] Falha ao ler do socket CAN: " << strerror(errno) << std::endl;
            g_running = false;
        }
        else {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    }
}

int main() {
    initscr();
    cbreak();
    noecho();
    keypad(stdscr, TRUE);
    nodelay(stdscr, TRUE);  // getch() não bloqueia
    start_color();

    // Define as cores
    init_pair(1, COLOR_CYAN,    COLOR_BLACK);   
    init_pair(2, COLOR_GREEN,   COLOR_BLACK);   
    init_pair(3, COLOR_YELLOW,  COLOR_BLACK);   
    init_pair(4, COLOR_RED,     COLOR_BLACK);   
    init_pair(5, COLOR_MAGENTA, COLOR_BLACK);   
    init_pair(6, COLOR_BLUE,    COLOR_BLACK);   
    init_pair(7, COLOR_WHITE,   COLOR_BLACK);   

    WINDOW *dashboardWin = newwin(LINES, COLS, 0, 0);
    showBanner(dashboardWin);
    napms(1000);

    werase(dashboardWin);
    wattron(dashboardWin, COLOR_PAIR(3));
    wprintw(dashboardWin, "[INFO] Abrindo interface CAN can0...\n");
    wattroff(dashboardWin, COLOR_PAIR(3));
    wrefresh(dashboardWin);

    g_canSocket = openCAN("can0");

    werase(dashboardWin);
    wattron(dashboardWin, COLOR_PAIR(2));
    wprintw(dashboardWin, "[OK] Interface CAN can0 aberta com sucesso!\n");
    wattroff(dashboardWin, COLOR_PAIR(2));
    wrefresh(dashboardWin);
    napms(1000);

    std::thread canReceiveThread(canReceiveThreadFunc);

    while (g_running) {
        displayDashboard(dashboardWin, lastCanMsg);
        napms(100);

        int ch = getch();
        if (ch == 'q' || ch == 'Q') {
            g_running = false;
        }
    }

    if (canReceiveThread.joinable())
        canReceiveThread.join();

    werase(dashboardWin);
    wattron(dashboardWin, COLOR_PAIR(4));
    wprintw(dashboardWin, "\nEncerrando o programa...\n");
    wattroff(dashboardWin, COLOR_PAIR(4));
    wrefresh(dashboardWin);
    close(g_canSocket);
    napms(1000);

    endwin();
    return 0;
}

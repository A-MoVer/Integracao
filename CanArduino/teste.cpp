#include <iostream>
#include <boost/asio.hpp>
#include <string>
#include <json/json.h> // Biblioteca para manipular JSON

void exibirEstadoFormatado(const std::string &jsonString) {
    Json::CharReaderBuilder leitorJSON;
    Json::Value dados;
    std::string erros;

    //teste

    // Parse do JSON recebido
    std::istringstream entrada(jsonString);
    if (Json::parseFromStream(leitorJSON, entrada, &dados, &erros)) {
        // Interface formatada
        std::cout << "=============================" << std::endl;
        std::cout << "       ESTADO DOS BOTOES     " << std::endl;
        std::cout << "=============================" << std::endl;
        std::cout << "Botão A: " << (dados["buttonA"].asBool() ? "Premido" : "Solto") << std::endl;
        std::cout << "Botão B: " << (dados["buttonB"].asBool() ? "Premido" : "Solto") << std::endl;
        std::cout << "=============================" << std::endl;
    } else {
        std::cerr << "Erro ao processar JSON: " << erros << std::endl;
    }
}

int main() {
    try {
        // Configuração da porta serial
        boost::asio::io_service io;
        boost::asio::serial_port portaSerial(io, "/dev/ttyACM0"); // Substitua pela porta correta do Arduino
        portaSerial.set_option(boost::asio::serial_port_base::baud_rate(9600));

        boost::asio::streambuf buffer;

        std::cout << "Ligação estabelecida. A receber dados...\n";

        while (true) {
            // Ler até o final da linha
            boost::asio::read_until(portaSerial, buffer, '\n');
            std::istream fluxo(&buffer);
            std::string linha;
            std::getline(fluxo, linha);

            // Exibir os dados formatados
            exibirEstadoFormatado(linha);
        }
    } catch (std::exception &e) {
        std::cerr << "Erro: " << e.what() << std::endl;
        return 1;
    }

    return 0;
}

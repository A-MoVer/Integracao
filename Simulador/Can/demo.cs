using System;
using IXXAT;
using static IXXAT.SimplyCAN;

namespace CAN
{
  class Demo
  {
    private static char[] mserial_port = "COM5\0".ToCharArray();
    private static ushort mbitrate = 500;

    private static void ErrorExit()
    {
      Console.WriteLine("Erro: " + simply_get_last_error());
      simply_close();
      Environment.Exit(-1);
    }

    private static void InterpretarMensagem(can_msg_t msg)
    {
      if (msg.dlc < 4) return;

      byte status = msg.payload[0];
      int distancia = msg.payload[1] + (msg.payload[2] << 8);
      string lado = msg.payload[3] == 1 ? "Direita" : "Esquerda";

      string algoritmo = msg.ident switch
      {
        0x100 => "FrontalCollision",
        0x110 => "RearCollision",
        0x120 => "BlindSpotDetection",
        0x105 => "PedestrianDetection",
        _ => $"Algoritmo desconhecido (ID: 0x{msg.ident:X})"
      };

      string perigo = status == 1 ? "PERIGO" : "Seguro";

      Console.WriteLine($"[ALERTA] {algoritmo} | Status: {perigo} | Distância: {distancia} cm | Lado: {lado}");
    }

    private static void ReceberMensagens()
    {
      can_msg_t rx = new can_msg_t();

      while (!Console.KeyAvailable)
      {
        sbyte res = simply_receive(ref rx);
        if (res == 1)
        {
          InterpretarMensagem(rx);
        }
        else if (res == -1)
        {
          ErrorExit();
        }

        System.Threading.Thread.Sleep(1);
      }
    }

  }
}

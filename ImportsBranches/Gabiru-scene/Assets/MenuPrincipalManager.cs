using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelMultiplayer;

    public void Jogar()
    {
        painelMenuInicial.SetActive(false);
        painelMultiplayer.SetActive(true);
    }

    public void Fechar()
    {
        painelMultiplayer.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void Sair()
    {
        Debug.Log("Sair do Jogo");
        Application.Quit();
    }
}
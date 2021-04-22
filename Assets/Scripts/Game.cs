using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Playables;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum EmployeeState
{
    Working = 0,
    Snoozing = 1,
    Dead = 2
}

public enum BossState
{
    Roaming = 0,
    Knocking = 1,
    InRoom = 2,
    FiredYou = 3
}

public class Game : MonoBehaviour
{

    [Header("Game")]
    [SerializeField] private Image m_TimeMeter;
    [SerializeField] private float m_GameOverTime = 60f;
    [SerializeField] private float m_PatrolPercentage = 0.12f;
    [SerializeField] private GameObject m_CountDown;
    [SerializeField] private Meter m_SnoozeMeter;
    [SerializeField] private GameObject m_WinUI;
    [SerializeField] private GameObject m_GameOverUI;
    [SerializeField] private GameObject m_FatigueUI;
    private float m_GameTime;
    private bool m_GameStarted;
    private bool m_StopGame;

    [Header("Audio")]
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField] private AudioClip m_KnockSound;
    [SerializeField] private AudioClip m_OpenSound;
    [SerializeField] private AudioClip m_CloseSound;
    [SerializeField] private AudioClip m_WinSound;
    [SerializeField] private AudioClip m_LoseSound;
    [SerializeField] private AudioClip m_DieSound;

    [Header("Boss")]
    [SerializeField] private GameObject m_Boss;
    [SerializeField] private GameObject m_Warning;
    [SerializeField] private GameObject m_BossGreeting;
    private Animator m_BossAnimator;
    private BossState m_BossState;
    private bool m_StartBossPattern;
    private bool m_FirstCheck;
    private IEnumerator m_BossCoroutine;

    [Header("Employee")]
    [SerializeField] private GameObject m_Employee;
    [SerializeField] private GameObject m_EmployeeGreeting;
    [SerializeField] private float m_WakeUpTime = 3f;
    [SerializeField] private float m_SnoozeNeeded = 4000f;
    [SerializeField] private float m_SnoozeReduceSpeed = 1f;
    [SerializeField] private float m_AllowReSnoozeBuffer = 0.15f;
    private Animator m_EmployeeAnimator;
    private IEnumerator m_SnoozeCoroutine;
    private EmployeeState m_EmployeeState;
    private float m_SnoozeAmount;
    private bool m_PressedSnooze;
    private bool m_AllowSnooze;
    private bool m_AllowReSnooze;

    [Header("Coworker")]
    [SerializeField] private GameObject m_CoworkerGreeting;

    private void Awake()
    {
        Time.timeScale = 1;

        m_StopGame = false;
        m_GameStarted = false;
        m_StartBossPattern = false;
        m_FirstCheck = false;

        m_Warning.SetActive(false);
        m_BossGreeting.SetActive(false);
        m_EmployeeGreeting.SetActive(false);
        m_CoworkerGreeting.SetActive(false);

        StartCoroutine(Countdown());

        m_BossState = BossState.Roaming;
        m_BossAnimator = m_Boss.GetComponent<Animator>();

        m_EmployeeState = EmployeeState.Working;
        m_EmployeeAnimator = m_Employee.GetComponent<Animator>();

        m_SnoozeCoroutine = Snoozing();
        m_BossCoroutine = BossPattern();

        m_FatigueUI.SetActive(false);
        m_GameOverUI.SetActive(false);
        m_WinUI.SetActive(false);
    }

    private IEnumerator Countdown()
    {
        m_CountDown.SetActive(true);
        Time.timeScale = 0;

        yield return new WaitForSecondsRealtime(3);

        Time.timeScale = 1;
        m_GameStarted = true;
        m_AllowSnooze = true;
        m_StartBossPattern = true;
        m_FirstCheck = true;

        yield return new WaitForSecondsRealtime(1);

        m_CountDown.SetActive(false);
    }

    private void Start()
    {
        m_SnoozeMeter.UpdateMeter(m_SnoozeAmount / m_SnoozeNeeded);
    }

    private void Update()
    {
        if(m_GameStarted && !m_StopGame)
        {
            PlayGame();
        }
    }

    private void PlayGame()
    {
        IncreaseTimeProgress();

        if(m_SnoozeAmount >= m_SnoozeNeeded)
        {
            StartCoroutine(WinGame());
        }

        if(m_GameTime >= m_GameOverTime)
        {
            StartCoroutine(FatigueGameOver());
        }

        if(m_StartBossPattern)
        {
            // If the player has been sleeping enough for the boss to get suspicous
            if(m_SnoozeAmount >  m_SnoozeNeeded * m_PatrolPercentage)
            {
                m_StartBossPattern = false;
                StartCoroutine(m_BossCoroutine);
            }
        }

        // If the player pressed the sleep button
        if (m_PressedSnooze)
        {
            if (m_AllowSnooze)
            {
                m_AllowSnooze = false;
                m_AllowReSnooze = false;
                StartCoroutine(m_SnoozeCoroutine);
            }
            else if (m_AllowReSnooze)
            {
                // This happens when the player pressed the snooze button and can resnooze
                m_AllowSnooze = false;
                m_AllowReSnooze = false;
                StopCoroutine(m_SnoozeCoroutine);
                m_SnoozeCoroutine = Snoozing();
                StartCoroutine(m_SnoozeCoroutine);
            }
        }

        // The state of the player (employee)
        switch (m_EmployeeState)
        {
            case EmployeeState.Working:

                // If the player is working reduce the amount of sleep you've gathered 
                if (m_SnoozeAmount < m_SnoozeNeeded)
                {
                    ReduceSnooze();
                }

                break;
            case EmployeeState.Snoozing:

                // If the player is sleeping and the boss is and the room end the game
                if(m_BossState == BossState.InRoom)
                {
                    StartCoroutine(FiredGameOver());
                }

                break;
            case EmployeeState.Dead:

                break;
            default:
                break;
        }
    }

    private IEnumerator BossPattern()
    {
        // Set random check time
        float randomCheckTime = Random.Range(8, 12);
        // Set random leave time
        float randomLeaveTime = Random.Range(4, 8); 

        if (m_FirstCheck)
        {
            // Divide the random check time by 2 the first time the boss starts patrolling 
            m_FirstCheck = false;
            randomCheckTime *= 0.5f;
        }

        // When the random check time has passed
        yield return new WaitForSeconds(randomCheckTime);

        // Convert the boss state (enum) to an int
        m_BossState = BossState.Knocking;
        m_BossAnimator.SetInteger("State", (int)m_BossState);

        AudioSource.PlayClipAtPoint(m_KnockSound, m_Boss.transform.position);
        m_Warning.SetActive(true);

        // Enter room after knocking delay
        yield return new WaitForSeconds(3f); 

        AudioSource.PlayClipAtPoint(m_OpenSound, m_Boss.transform.position);

        m_BossState = BossState.InRoom;
        m_BossAnimator.SetInteger("State", (int)m_BossState);

        m_Warning.SetActive(false);

        if(randomLeaveTime >= 7)
        {
            // Set little greeting UI active :D (also indicates long leave time)
            yield return new WaitForSeconds(1);
            randomLeaveTime -= 1;
            m_BossGreeting.SetActive(true);

            yield return new WaitForSeconds(2);
            randomLeaveTime -= 2;
            m_EmployeeGreeting.SetActive(true);
            m_CoworkerGreeting.SetActive(true);
        }

        yield return new WaitForSeconds(randomLeaveTime);

        // The boss has left the room
        m_BossGreeting.SetActive(false);
        m_EmployeeGreeting.SetActive(false);
        m_CoworkerGreeting.SetActive(false);

        AudioSource.PlayClipAtPoint(m_CloseSound, m_Boss.transform.position);

        m_BossState = BossState.Roaming;
        m_BossAnimator.SetInteger("State", (int)m_BossState);

        // Restart the boss pattern coroutine
        m_BossCoroutine = BossPattern();
        StartCoroutine(m_BossCoroutine);
    }

    private IEnumerator Snoozing()
    {
        m_EmployeeState = EmployeeState.Snoozing;
        // Convert the state (enum) of the player to an int
        m_EmployeeAnimator.SetInteger("State", (int)m_EmployeeState); 

        // Increase the amount of snooze the player has had
        m_SnoozeAmount = Mathf.Clamp(m_SnoozeAmount + 1, 0, m_SnoozeNeeded);
        // Update the size of the snooze bar by calculating the current progress
        m_SnoozeMeter.UpdateMeter(m_SnoozeAmount / m_SnoozeNeeded);

        yield return new WaitForSeconds(m_AllowReSnoozeBuffer);

        // The player can cancel waking up
        m_AllowReSnooze = true;

        yield return new WaitForSeconds(m_WakeUpTime);

        // The player woke up
        m_AllowSnooze = true;

        m_EmployeeState = EmployeeState.Working;
        m_EmployeeAnimator.SetInteger("State", (int)m_EmployeeState);

        m_SnoozeCoroutine = Snoozing();
    }

    private IEnumerator FiredGameOver()
    {
        m_BossGreeting.SetActive(false);
        m_EmployeeGreeting.SetActive(false);
        m_CoworkerGreeting.SetActive(false);

        m_BossState = BossState.FiredYou;

        m_StopGame = true;

        Time.timeScale = 0;
        m_AudioSource.Stop();

        yield return new WaitForSecondsRealtime(1f);

        m_BossAnimator.SetInteger("State", (int)m_BossState);
        AudioSource.PlayClipAtPoint(m_LoseSound, Vector2.zero);

        yield return new WaitForSecondsRealtime(2);

        m_GameOverUI.SetActive(true);
    }

    private IEnumerator FatigueGameOver()
    {
        m_BossGreeting.SetActive(false);
        m_EmployeeGreeting.SetActive(false);
        m_CoworkerGreeting.SetActive(false);

        m_StopGame = true;

        Time.timeScale = 0;
        m_AudioSource.Stop();

        m_EmployeeState = EmployeeState.Dead;
        m_EmployeeAnimator.SetInteger("State", (int)m_EmployeeState);
        AudioSource.PlayClipAtPoint(m_DieSound, Vector2.zero);

        yield return new WaitForSecondsRealtime(2f);

        m_FatigueUI.SetActive(true);
    }

    private IEnumerator WinGame()
    {
        m_StopGame = true;

        Time.timeScale = 0;
        m_AudioSource.Stop();
        AudioSource.PlayClipAtPoint(m_WinSound, Vector2.zero);

        yield return new WaitForSecondsRealtime(0.5f);

        m_GameOverUI.SetActive(false);
        m_FatigueUI.SetActive(false);
        m_WinUI.SetActive(true);
    }

    public void PressedSnoozeInfo(InputAction.CallbackContext context)
    {
        m_PressedSnooze = context.started;
    }

    private void ReduceSnooze()
    {
        m_SnoozeAmount = Mathf.Clamp(m_SnoozeAmount - Time.deltaTime * m_SnoozeReduceSpeed, 0, m_SnoozeNeeded);
        m_SnoozeMeter.UpdateMeter(m_SnoozeAmount / m_SnoozeNeeded);
    }

    private void IncreaseTimeProgress()
    {
        m_GameTime = Mathf.Clamp(m_GameTime + Time.deltaTime, 0, m_SnoozeNeeded);
        m_TimeMeter.fillAmount = m_GameTime / m_GameOverTime;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}


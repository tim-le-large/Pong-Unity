using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
    public GameObject ball;
    public GameObject ballSprite;
    public GameObject player;
    public GameObject scoreAgent;
    public GameObject scorePlayer;
    public Slider brainSlider;
    public float maxSpeed;

    private float _speed;

    private readonly IDictionary<string, int> _states = new Dictionary<string, int>()
    {
        {"ballX", SizeX/2},
        {"ballY", SizeY/2},
        {"paddleY", SizeY/2 - SizePaddle},
        {"velX", 1},
        {"velY", 1},
    };

    private const int  SizeX = 30;
    private const int SizeY = 28;
    private const int SizePaddle = 4;
    private int[] _maxStates;
    private IDictionary<int, float[]> _qTable = new Dictionary<int, float[]>();
    private bool _moveUp;
    private int _currState;
    private int _episodes;
    private int _error;
    private bool _pressedUp;
    private bool _pressedDown;
    private int _scoreAgent ;
    private int _scorePlayer;
    private int _paddleYPlayer = SizeY/2 -SizePaddle;
    private TextMeshProUGUI _textMeshProUGUI;
    private TextMeshProUGUI _textMeshProUGUI1;
    private SpriteRenderer _spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = ballSprite.GetComponent<SpriteRenderer>();
        _textMeshProUGUI1 = scorePlayer.GetComponent<TextMeshProUGUI>();
        _textMeshProUGUI = scoreAgent.GetComponent<TextMeshProUGUI>();
        _maxStates = new [] {SizeX, SizeY, SizeY, 2, 2};
        UseBrain(8f);
        _speed = maxSpeed;
        scoreAgent.transform.position = new Vector3(SizeX / 4f, SizeY-1, -3);
        scorePlayer.transform.position = new Vector3((SizeX / 4)*3, SizeY-1, -3);

        brainSlider.onValueChanged.AddListener((UseBrain));

    }

    // Update is called once per frame
    private void Update()
    {

        if (Input.GetKey(KeyCode.UpArrow))
        {
            _pressedUp = true;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            _pressedDown = true;
        }
        _speed += Time.deltaTime;
        if (_speed >= maxSpeed)
        {
            //update scores

            _textMeshProUGUI.text = Convert.ToString(_scoreAgent);
            _textMeshProUGUI1.text = Convert.ToString(_scorePlayer);
            // move player
            if (_pressedUp && _paddleYPlayer < SizeY-SizePaddle)
            {
                _paddleYPlayer += 1;
                player.transform.position = new Vector3(SizeX-0.15f, _paddleYPlayer, -2);
                _pressedUp = false;
            }
            if (_pressedDown && _paddleYPlayer > 0)
            {
                _paddleYPlayer -= 1;
                player.transform.position = new Vector3(SizeX-0.15f, _paddleYPlayer, -2);
                _pressedDown = false;
            }

            _currState = Observation();
            _moveUp = Decision(_currState);
            Action(_moveUp);
            Reward();
            _speed = 0;



        }
        //smooth update ball
        var dest = new Vector3(_states["ballX"], _states["ballY"], -1);
        var origin = ball.transform.position;
        ball.transform.position = Vector3.Lerp(origin, dest, _speed / maxSpeed);




    }


    private int Observation()
    {
        return GetState(_states);
    }

    private bool Decision(int currState)
    {
        return _qTable[currState][0] < _qTable[currState][1];
    }

    private void Action(bool moveUp)
    {
        MovePaddle(moveUp);
        MoveBall();
        UpdateDirection();

    }






    private void MoveBall()
    {
        // ball_x
        if (_states["velX"] == 0)
        {
            _states["ballX"] -= 1;
        }
        else
        {
            _states["ballX"] += 1;
        }
        //ball_y
        if (_states["velY"] == 0)
        {
            _states["ballY"] -= 1;
        }
        else
        {
            _states["ballY"] += 1;
        }

        // ball.transform.position = Vector3.Lerp(ball.transform.position, new Vector3(_states["ballX"], _states["ballY"], -1), Time.deltaTime);


    }

    private void MovePaddle(bool moveUp)
    {
        if (moveUp)
        {
            _states["paddleY"] += 1;
        }
        else
        {
            _states["paddleY"] -= 1;
        }
        if (_states["paddleY"] < 0)
        {
            _states["paddleY"] = 0;
        }
        if (_states["paddleY"] > SizeY-SizePaddle)
        {
            _states["paddleY"] = SizeY-SizePaddle;
        }
        transform.position= new Vector3(0,_states["paddleY"],  -2);
    }

    private void UpdateDirection()
    {
        if (_states["ballX"] == 0)
        {
            _states["velX"] = Convert.ToInt32(!Convert.ToBoolean(_states["velX"]));
        }
        if (_states["ballX"] == SizeX-1)
        {
            _states["velX"] = Convert.ToInt32(!Convert.ToBoolean(_states["velX"]));
        }
        if (_states["ballY"] == 0)
        {
            _states["velY"] = Convert.ToInt32(!Convert.ToBoolean(_states["velY"]));
        }
        if (_states["ballY"] == SizeY-1)
        {
            _states["velY"] = Convert.ToInt32(!Convert.ToBoolean(_states["velY"]));
        }
    }

    private void Reward()
    {
        if (_states["ballX"] == 0)
        {
            if (_states["ballY"] >=_states["paddleY"] && _states["ballY"] <_states["paddleY"] + SizePaddle)
            {
                _spriteRenderer.color = Color.green;
                // UpdateQTable(1);
            }
            else
            {
                // UpdateQTable(-1);
                _spriteRenderer.color = Color.red;
                _error += 1;
                _scorePlayer += 1;
            }

            _episodes += 1;
            if (_episodes % 100 == 0)
            {
                // print("Error: "+ _error +"%" );
                // var jsonQTable = JsonConvert.SerializeObject( _qTable );
                // switch (_error)
                // {
                //         case(>40):
                //             File.WriteAllText("40.txt", jsonQTable);
                //             break;
                //         case(>35):
                //             File.WriteAllText("35.txt", jsonQTable);
                //             break;
                //         case(>30):
                //             File.WriteAllText("30.txt", jsonQTable);
                //             break;
                //         case(>25):
                //             File.WriteAllText("25.txt", jsonQTable);
                //             break;
                //         case(>20):
                //             File.WriteAllText("20.txt", jsonQTable);
                //             break;
                //         case(>15):
                //             File.WriteAllText("15.txt", jsonQTable);
                //             break;
                //         case(>10):
                //             File.WriteAllText("10.txt", jsonQTable);
                //             break;
                //         case(>5):
                //             File.WriteAllText("5.txt", jsonQTable);
                //             break;
                //         case(0):
                //             File.WriteAllText("0.txt", jsonQTable);
                //             break;
                // }

                _error = 0;

            }

        }
        else
        {
            // UpdateQTable(0);
        }
        // player score
        if (_states["ballX"] == SizeX-1)
        {
            if  (_states["ballY"] < _paddleYPlayer || _states["ballY"] > _paddleYPlayer + SizePaddle)
            {
                _scoreAgent += 1;
                _spriteRenderer.color = Color.red;

            }
            else
            {
                _spriteRenderer.color = Color.green;

            }
        }
    }

    private void UpdateQTable(int reward)
    {
        var nextState = GetState(_states);
        var maxNextState = Math.Max(_qTable[nextState][0], _qTable[nextState][1]);
        var qCurrState = _qTable[_currState][Convert.ToInt32(_moveUp)];
        _qTable[_currState][Convert.ToInt32(_moveUp)] =qCurrState+  0.1f * (reward + 0.9f * maxNextState - qCurrState);

    }

    private int GetState(IDictionary<string, int> currStates)
    {
        var state = currStates.Values.ElementAt(0);
        for (var i = 1; i < currStates.Count; i++)
        {
            state = state * _maxStates[i] + currStates.Values.ElementAt(i);
        }

        return state;
    }

    private IDictionary<int,float[]> InitQ()
    {
        IDictionary<int, float[]> qTable = new Dictionary<int, float[]>();
        foreach (var ballX in Enumerable.Range(0, _maxStates[0]))
        {
            foreach (var ballY in Enumerable.Range(0, _maxStates[1]))
            {
                foreach (var paddleY in Enumerable.Range(0, _maxStates[2]))
                {
                    foreach (var velX in Enumerable.Range(0, _maxStates[3]))
                    {
                        foreach (var velY in Enumerable.Range(0, _maxStates[4]))
                        {
                            var state = GetState( new Dictionary<string, int>()   {
                                {"ballX", ballX},
                                {"ballY", ballY},
                                {"paddleY", paddleY},
                                {"velX", velX},
                                {"velY", velY},
                            });
                            var move1 = Random.Range(-0.1f, 0.1f);
                            var move2 = Random.Range(-0.1f, 0.1f);
                            var actions = new [] {move1, move2};
                            qTable.Add(state, actions);
                        }
                    }
                }
            }
        }

        return qTable;


    }

    private void UseBrain(float error)
    {
        var path = Path.Combine(Application.streamingAssetsPath, ""+(error * 5) + ".txt");
        StartCoroutine(GetRequest(path));
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.Success:
                    _qTable = JsonConvert.DeserializeObject<IDictionary<int, float[]>>(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

}

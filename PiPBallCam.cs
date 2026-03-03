using BepInEx;
using UnityEngine;
using System.Linq;

[BepInPlugin("com.kingcox.sbg.pipballcam", "SBG PiP Ball Cam", "5.2.0")]
public class BallCamMod : BaseUnityPlugin
{
    private Camera _ballCam;
    private GameObject _ballCamObj;
    private Vector3 _offset = new Vector3(0, 1.2f, -3.0f);
    private float _smoothSpeed = 14f;
    
    private float _movementThreshold = 0.02f; 
    private float _lingerTime = 1.5f;        
    private float _lastMoveTime;
    private string _currentTargetName = "";

    void Update()
    {
        if (Camera.main == null || GameManager.LocalPlayerInfo == null)
        {
            ClosePip();
            return;
        }

        PlayerInfo targetPlayer = GetActualSpectatorTarget();

        if (targetPlayer != null && targetPlayer.AsGolfer?.OwnBall != null)
        {
            var targetBall = targetPlayer.AsGolfer.OwnBall;
            _currentTargetName = targetPlayer.Name;
            
            float speed = (targetBall.Rigidbody != null) ? targetBall.Rigidbody.linearVelocity.magnitude : 0f;

            if (speed > _movementThreshold || (Time.time - _lastMoveTime < _lingerTime))
            {
                if (speed > _movementThreshold) _lastMoveTime = Time.time;
                ShowPip(targetBall);
            }
            else
            {
                ClosePip();
            }
        }
        else
        {
            ClosePip();
        }
    }

    private PlayerInfo GetActualSpectatorTarget()
    {
        var lp = GameManager.LocalPlayerInfo;

        // If we aren't spectating, the target is ourselves
        if (lp.AsSpectator == null || !lp.AsSpectator.IsSpectating)
        {
            return lp;
        }

        // If we ARE spectating, find the player whose ID matches the spectator target
        // Some versions of SBG use 'TargetPlayerId' or 'TargetPlayer'
        if (lp.AsSpectator.TargetPlayer != null) 
        {
            return lp.AsSpectator.TargetPlayer;
        }

        // Fallback: If the game uses a different internal pointer, we find the player 
        // that matches the Name currently displayed in the Spectator UI
        var specUI = Object.FindAnyObjectByType<SpectatorUI>();
        if (specUI != null && specUI.TargetPlayerNameText != null)
        {
            string viewingName = specUI.TargetPlayerNameText.text;
            return Object.FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None)
                         .FirstOrDefault(p => p.Name == viewingName);
        }

        return null;
    }

    private void OnGUI()
    {
        if (_ballCamObj != null && _ballCamObj.activeSelf)
        {
            float pipWidth = Screen.width * 0.22f;
            float pipHeight = Screen.height * 0.22f;
            float pipX = Screen.width * 0.02f;
            float pipY = Screen.height - (Screen.height * 0.02f) - pipHeight;

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 18;
            labelStyle.alignment = TextAnchor.LowerCenter;
            labelStyle.fontStyle = FontStyle.Bold;

            // Shadow
            Rect shadowRect = new Rect(pipX + 1, pipY + pipHeight - 24, pipWidth, 25);
            labelStyle.normal.textColor = Color.black;
            GUI.Label(shadowRect, _currentTargetName, labelStyle);

            // White Text
            Rect textRect = new Rect(pipX, pipY + pipHeight - 25, pipWidth, 25);
            labelStyle.normal.textColor = Color.white;
            GUI.Label(textRect, _currentTargetName, labelStyle);
        }
    }

    private void ShowPip(GolfBall ball)
    {
        if (_ballCamObj == null) CreateBallCamera();
        if (!_ballCamObj.activeSelf) _ballCamObj.SetActive(true);

        Vector3 ballPos = ball.transform.position;
        Vector3 targetPos = ballPos + _offset;

        _ballCam.transform.position = Vector3.Lerp(_ballCam.transform.position, targetPos, Time.deltaTime * _smoothSpeed);
        _ballCam.transform.LookAt(ballPos + Vector3.up * 0.1f);
    }

    private void ClosePip()
    {
        if (_ballCamObj != null && _ballCamObj.activeSelf) _ballCamObj.SetActive(false);
    }

    private void CreateBallCamera()
    {
        _ballCamObj = new GameObject("PiPBallCamera");
        _ballCam = _ballCamObj.AddComponent<Camera>();
        _ballCam.rect = new Rect(0.02f, 0.02f, 0.22f, 0.22f); 
        _ballCam.depth = 999; 
        _ballCam.cullingMask = ~(1 << 5); 
        DontDestroyOnLoad(_ballCamObj);
    }
}
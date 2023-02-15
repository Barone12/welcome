using System.Collections;
using UnityEngine;

// 총을 구현한다
public class Gun : MonoBehaviour {

    // 총의 상태를 표현하는데 사용할 타입을 선언한다

    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }
    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magCapacity = 25; // 탄창 용량
    public int magAmmo; // 현재 탄창에 남아있는 탄약


    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간
    private float lastFireTime; // 총을 마지막으로 발사한 시점


    private void Awake() {
        // 사용할 컴포넌트들의 참조를 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();
        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;

    }

    private void OnEnable() {
        // 총 상태 초기화

        //현재 탄창을 가득 채우기
        magAmmo = magCapacity;
        //총의 현재 상태를 총을 쏠 준비가 된 상태로 변경
        state = State.Ready;
        //마지막으로 총을 쏜 시점 초기화
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire() {
        //현재 상태가 발사 가능한 상태이고
        // 마지막 총 발사 시점에서 timeBetFire 이상의 시간이 지나야한다.
        if (state == State.Ready && Time.time >= lastFireTime + timeBetFire) {
            lastFireTime = Time.time;
            Shot();

        }
    }
    // 실제 발사 처리
    private void Shot() {
        //레이캐스트에 의한 충돌 정보를 저장하는 컨테이너
        RaycastHit hit;
        //탄알이 맞은 곳을 저장할 변수;
        Vector3 hitPosition = Vector3.zero;

        //레이캐스트(시작 지점, 방향, 충돌 정보 컨테이너, 사정거리)
        //Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitinfo, float maxDistance)
        if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            //상대방으로부터 IDamaeable 오브젝트 가져오기 시도
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            //가져오는 데 성공했다면
            if (target != null)
            {
                //상대방의 OnDamage를 실행시켜 대미지 주기
                target.OnDamage(damage, hit.point, hit.normal);

            }
            //레이가 충돌한 위치 저장
            hitPosition = hit.point;
            
        }
        else {
            //레이가 다른 물체와 충돌하지 않았다면
            //탄알이 최대 사정거리까지 날라갔을 때의 위치를 충돌 위치로 사용
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        
        }
        StartCoroutine(ShotEffect(hitPosition));
        //한 발 싼다잇
        magAmmo--;
        if (magAmmo <= 0) {
            state = State.Empty;
        }

    }

    // 발사 이펙트와 소리를 재생하고 총알 궤적을 그린다
    private IEnumerator ShotEffect(Vector3 hitPosition) {

        //총구 화염 효과 재생
        muzzleFlashEffect.Play();
        //탄피 배출 효과 재생
        shellEjectEffect.Play();
        //총격 소리 재생
        gunAudioPlayer.PlayOneShot(shotClip);//중첩재생
        //선의 시작점은 총구 위치
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        //선의 끝점은 입력으로 들어온 충돌위치
        bulletLineRenderer.SetPosition(1,hitPosition);
       


        // 라인 렌더러를 활성화하여 총알 궤적을 그린다
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 총알 궤적을 지운다
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);
        
        // 재장전 소요 시간 만큼 처리를 쉬기
        yield return new WaitForSeconds(reloadTime);
        //탄창에 채울 탄약 계산
        int ammoToFill = magCapacity - magAmmo;

        if (ammoRemain < ammoToFill) {
            ammoToFill = ammoRemain;
        }

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}
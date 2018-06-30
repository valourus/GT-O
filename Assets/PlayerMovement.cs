using UnityEngine;
using UnityEngine.Networking;

public class PlayerMovement : NetworkBehaviour
{
	private Rigidbody rb;
	private float force { get; set; }
	private float jumpForce { get; set; }
	private bool isOnGround { get; set; }
	
	public void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.useGravity = true;
		force = 11.5f;
		jumpForce = 10;
		isOnGround = true;
		rb.maxAngularVelocity = 2;
	}
	
	public void FixedUpdate() {
		rb.AddForce(Physics.gravity, ForceMode.Acceleration);
	}
	
	private void Update()
	{
		if (!hasAuthority) return;
		if (Input.GetKey(KeyCode.A))
			rb.AddForce(Vector3.left * force);
		if (Input.GetKey(KeyCode.D))
			rb.AddForce(Vector3.right * force);
		if (Input.GetKey(KeyCode.Space) && isOnGround)
		{
			rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
			isOnGround = false;
		}

		if (Input.GetKey(KeyCode.S))
			rb.AddForce(Vector3.down * force);
	}
	
	private void OnCollisionEnter(Collision other)
	{

		if(other.gameObject.tag.Equals("ground"))
			isOnGround = true;
		else
		{
			//trigger a explotion that is synced on the network... 
		}
	}
}

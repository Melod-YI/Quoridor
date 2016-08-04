package model.po;

import model.vo.PlayerVO;

public class PlayerPO {
	private int playNo;
	private int wallLeft;
	
	public PlayerPO(int playNo,int wallLeft){
		this.setPlayNo(playNo);
		this.setWallLeft(wallLeft);
	}
	
	
	public PlayerVO getDisplayPlayer(){
		PlayerVO pvo =new PlayerVO(playNo,wallLeft);
		return pvo;
	}

	public int getPlayNo() {
		return playNo;
	}

	public void setPlayNo(int playNo) {
		this.playNo = playNo;
	}

	public int getWallLeft() {
		return wallLeft;
	}

	public void setWallLeft(int wallLeft) {
		this.wallLeft = wallLeft;
	}

}

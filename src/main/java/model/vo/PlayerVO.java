package model.vo;

import java.io.Serializable;

public class PlayerVO implements Serializable{
	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;
	private int playNo;
	private int wallLeft;
	
	public PlayerVO(int playNo,int wallLef){
		super();
		this.setPlayNo(playNo);
		this.setWallLeft(wallLeft);
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

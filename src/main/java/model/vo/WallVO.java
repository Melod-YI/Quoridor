package model.vo;

import java.io.Serializable;

import abstracter.WallDirection;
import model.state.WallState;
/**
 * 显示在前台的网格
 * @author Administrator
 *
 */
public class WallVO implements Serializable{

	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;
	private WallState state;
	private int x;
	private int y;
	private WallDirection direction;
	
	public WallVO(WallState state,int x,int y,WallDirection direction){
		super();
		this.setState(state);
		this.setX(x);
		this.setY(y);
		this.setDirection(direction);
	}

	public WallState getState() {
		return state;
	}

	public void setState(WallState state) {
		this.state = state;
	}

	public int getX() {
		return x;
	}

	public void setX(int x) {
		this.x = x;
	}

	public int getY() {
		return y;
	}

	public void setY(int y) {
		this.y = y;
	}

	public WallDirection getDirection() {
		return direction;
	}

	public void setDirection(WallDirection direction) {
		this.direction = direction;
	}
}

package model.vo;

import java.io.Serializable;

import model.state.BlockState;

/**
 * 前台显示的单元格
 * 
 * @author Administrator
 *
 */
public class BlockVO implements Serializable {

	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;
	private BlockState state;
	private int x;
	private int y;

	public BlockVO(BlockState state, int x, int y) {
		super();
		this.state = state;
		this.x = x;
		this.y = y;
	}

	public BlockState getState() {
		return state;
	}

	public void setState(BlockState state) {
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
}

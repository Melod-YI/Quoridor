package model.po;

import model.state.BlockState;
import model.vo.BlockVO;

/**
 * 后台处理的棋盘格单元
 * @author Administrator
 *
 */
public class BlockPO {
	private BlockState state;
	private int x;
	private int y;
	
	public BlockPO(BlockState state,int x,int y){
		this.setState(state);
		this.setX(x);
		this.setY(y);
	}
	
	public BlockVO getDisplayBlock(){
		BlockState bs = null;
		if(state==BlockState.red){
			bs=BlockState.red;
		}
		else if(state==BlockState.blue){
			bs=BlockState.blue;
		}
		else if(state==BlockState.yellow){
			bs=BlockState.yellow;
		}
		else if(state==BlockState.green){
			bs=BlockState.green;
		}
		else if(state==BlockState.empty){
			bs=BlockState.empty;
		}
		BlockVO bvo=new BlockVO(bs,x,y);
		return bvo;
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

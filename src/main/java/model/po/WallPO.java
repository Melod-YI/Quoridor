package model.po;

import model.state.WallState;
import model.vo.WallVO;
import abstracter.WallDirection;
/**
 * 后台处理的网格线
 * @author Administrator
 *
 */
public class WallPO {
	private WallState state;
	private int x;
	private int y;
	private WallDirection direction;
	
	public WallPO(WallState state,int x,int y,WallDirection direction){
		this.setState(state);
		this.setX(x);
		this.setY(y);
		this.setDirection(direction);
	}
	
	public WallVO getDisplayWall(){
		WallState ws=null;
		if(state==WallState.black){
			ws=WallState.black;
		}
		else if(state==WallState.red){
			ws=WallState.red;
		}
		WallDirection wd=null;
		if(direction==WallDirection.horizontal){
			wd=WallDirection.horizontal;
		}
		else if(direction==WallDirection.virtical){
			wd=WallDirection.virtical;
		}
		WallVO wvo=new WallVO(ws,x,y,wd);
		return wvo;
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

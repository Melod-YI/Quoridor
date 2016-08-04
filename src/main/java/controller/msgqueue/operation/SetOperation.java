package controller.msgqueue.operation;

import model.service.ChessBoardModelService;
import controller.msgqueue.OperationQueue;
import abstracter.WallDirection;


public class SetOperation extends PlayerOperation{
	private int playerNo;
	private int x;
	private int y;
	private WallDirection wallDirection;
	public SetOperation(int playerNo,int x,int y,WallDirection wallDirection){
		this.playerNo=playerNo;
		this.x=x;
		this.y=y;
		this.wallDirection=wallDirection;
	}
	@Override
	public void execute() {
		// TODO Auto-generated method stub
		ChessBoardModelService chess = OperationQueue.getChessBoardModel();
		chess.set(playerNo, x, y, wallDirection);
	}

}

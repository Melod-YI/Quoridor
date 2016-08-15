package controller.msgqueue.operation;

import controller.msgqueue.OperationQueue;
import model.service.ChessBoardModelService;
import abstracter.Direction;

public class SimpleMoveOperation extends PlayerOperation{
	private int playerNo;
	private Direction direction;
	
	
	public SimpleMoveOperation(int playerNo,Direction direction){
		this.direction=direction;
		this.playerNo=playerNo;
	}
	
	@Override
	public void execute() {
		// TODO Auto-generated method stub
		ChessBoardModelService chess = OperationQueue.getChessBoardModel();
		chess.simpleMove(playerNo, direction);
	}

}

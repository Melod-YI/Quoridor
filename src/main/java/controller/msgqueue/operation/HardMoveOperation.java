package controller.msgqueue.operation;

import abstracter.HardDirection;
import controller.msgqueue.OperationQueue;
import model.service.ChessBoardModelService;

public class HardMoveOperation extends PlayerOperation{
	private int playerNo;
	private HardDirection hardDirection;
	
	public HardMoveOperation(int playerNo, HardDirection hardDirection){
		this.playerNo=playerNo;
		this.hardDirection=hardDirection;
	}
	
	@Override
	public void execute() {
		// TODO Auto-generated method stub
		ChessBoardModelService chess = OperationQueue.getChessBoardModel();
		chess.hardMove(playerNo, hardDirection);
	}
	
}

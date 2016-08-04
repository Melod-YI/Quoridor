package controller.impl;

import abstracter.Direction;
import abstracter.WallDirection;
import controller.msgqueue.operation.MoveOperation;
import controller.msgqueue.operation.PlayerOperation;
import controller.msgqueue.operation.SetOperation;
import controller.service.GameControllerService;
import controller.msgqueue.OperationQueue;

public class GameControllerImpl implements GameControllerService{

	@Override
	public boolean handMove(int playerNo,Direction direction) {
		// TODO Auto-generated method stub
		PlayerOperation op=new MoveOperation(playerNo,direction);
		OperationQueue.addMineOperation(op);
		return true;
	}

	@Override
	public boolean handSet(int playerNo,int x, int y, WallDirection wallDirection) {
		// TODO Auto-generated method stub
		PlayerOperation op=new SetOperation(playerNo,x,y,wallDirection);
		OperationQueue.addMineOperation(op);
		return false;
	}

}

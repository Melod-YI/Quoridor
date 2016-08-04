package controller.msgqueue.operation;

import controller.msgqueue.OperationQueue;
import model.service.GameModelService;

public class StartGameOperation extends PlayerOperation{

	@Override
	public void execute() {
		// TODO Auto-generated method stub
		GameModelService game = OperationQueue.getGameModel();
		game.startGame();
	}
}

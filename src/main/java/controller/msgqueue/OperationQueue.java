package controller.msgqueue;

import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;

import controller.msgqueue.operation.PlayerOperation;
import model.service.ChessBoardModelService;
import model.service.GameModelService;


public class OperationQueue implements Runnable{
	
	private static BlockingQueue<PlayerOperation> queue;
	public static boolean isRunning;
	private static ChessBoardModelService chessBoard;
	private static GameModelService gameModel;
	
	public OperationQueue(ChessBoardModelService chess, GameModelService game){
		queue = new ArrayBlockingQueue<PlayerOperation>(1000);
		isRunning = true;
		
		chessBoard = chess;
		gameModel = game;
		
	}
	
	public static boolean singleUpdateSwitch = true;
	@Override
	public void run() {
		// TODO Auto-generated method stub
		while(isRunning){
			PlayerOperation operation = getNewPlayerOperation();
			operation.execute();
		}
	}

	private static PlayerOperation getNewPlayerOperation(){
		PlayerOperation  operation = null;
		try {
			operation = queue.take();
		} catch (InterruptedException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		return operation;
		
	}
	
	public static boolean addMineOperation (PlayerOperation operation){
		try {
			queue.put(operation);
		} catch (InterruptedException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
			return false;
		}
		return true;
	}
	
	public static ChessBoardModelService getChessBoardModel(){
		return chessBoard;
	}
	
	public static GameModelService getGameModel(){
		return gameModel;
	}
}

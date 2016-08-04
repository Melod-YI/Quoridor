package model.impl;

import model.impl.UpdateMessage;
import model.vo.GameVO;
import model.service.StatisticModelService;
import model.service.ChessBoardModelService;
import model.impl.BaseModel;
import model.service.GameModelService;
import model.state.GameResultState;
import model.state.GameState;

public class GameModelImpl extends BaseModel implements GameModelService{
	private StatisticModelService statisticModel;
	private ChessBoardModelService chessBoardModel;
	private GameResultState gameResultStae;
	private int time;
	private GameState gameState;
	private long startTime;
	int height;int width;int wallNum;int playerNum;
	
	public GameModelImpl(StatisticModelService statisticModel, ChessBoardModelService chessBoardModel){
		this.statisticModel = statisticModel;
		this.chessBoardModel = chessBoardModel;
		gameState = GameState.OVER;
		chessBoardModel.setGameModel(this);
	}
	@Override
	public boolean startGame() {
		// TODO Auto-generated method stub
		gameState = GameState.RUN;
		this.chessBoardModel.initialize( height, width,wallNum,playerNum);
		
		super.updateChange(new UpdateMessage("start",this.convertToDisplayGame()));
		return true;
	}

	private GameVO convertToDisplayGame(){
		return new GameVO(gameState,width,height);
	}
	
	@Override
	public boolean gameOver(GameResultState result) {
		// TODO Auto-generated method stub
		return false;
	}

	@Override
	public GameState getGameState() {
		// TODO Auto-generated method stub
		return null;
	}

}

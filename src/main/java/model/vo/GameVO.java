package model.vo;

import java.io.Serializable;

import model.state.GameState;

public class GameVO implements Serializable{

	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;
	private GameState gameState;
	private int width;
	private int height;
	
	public GameVO(GameState gameState,int width,int height){
		this.gameState=gameState;
		this.width=width;
		this.height=height;
	}
	public GameState getGameState() {
		return gameState;
	}
	public void setGameState(GameState gameState) {
		this.gameState = gameState;
	}
	public int getWidth() {
		return width;
	}
	public void setWidth(int width) {
		this.width = width;
	}
	public int getHeight() {
		return height;
	}
	public void setHeight(int height) {
		this.height = height;
	}
}
